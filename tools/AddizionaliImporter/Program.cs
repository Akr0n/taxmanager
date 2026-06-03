using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

// Importer dati addizionale comunale: unisce l'elenco comuni (region/provincia) con le aliquote MEF
// e genera data/addizionali/comuni-<anno>.json con tariffa a scaglioni + soglia di esenzione per ogni comune.

const decimal Top = 1_000_000_000_000m;
var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var outOpts = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = false };

var repoRoot = FindRepoRoot();
var toolsDir = Path.Combine(repoRoot, "tools");
var outDir = Path.Combine(repoRoot, "data", "addizionali");
Directory.CreateDirectory(outDir);

var comuni = JsonSerializer.Deserialize<List<RawComune>>(
    File.ReadAllText(Path.Combine(toolsDir, "comuni-raw.json")), web)!;
Console.WriteLine($"Comuni anagrafici: {comuni.Count}");

foreach (var year in new[] { 2024, 2025, 2026 })
{
    GenerateRegions(year);

    // Catena di fallback: per ogni comune si usa l'anno richiesto; se manca (delibera non ancora
    // pubblicata), si ripiega sull'ultima aliquota deliberata (anno-1, poi anno-2) = proroga.
    var mefChain = new[] { year, year - 1, year - 2 }
        .Select(y => Path.Combine(toolsDir, $"mef-{y}.csv"))
        .Where(File.Exists)
        .Select(ParseMef)
        .ToList();
    var outList = new List<OutComune>(comuni.Count);
    int matched = 0, withExemption = 0, withBrackets = 0, fromFallback = 0;

    foreach (var c in comuni)
    {
        var region = NormalizeRegion(c.Regione?.Nome ?? "", c.Sigla ?? "");
        var province = c.Provincia?.Nome ?? "";
        var name = c.Nome ?? "";
        var cc = (c.CodiceCatastale ?? "").Trim().ToUpperInvariant();

        List<OutBracket> brackets = new() { new(Top, 0m) };
        decimal exemption = 0m;
        for (int mi = 0; mi < mefChain.Count; mi++)
        {
            if (!mefChain[mi].TryGetValue(cc, out var info)) continue;
            // Entry presente ma SENZA aliquota (delibera dell'anno non ancora pubblicata): si ripiega
            // sull'ultima aliquota effettivamente deliberata (proroga). I comuni a 0% reale (es. Trento/
            // Bolzano) non hanno alcun anno "meaningful" e restano correttamente a 0.
            var meaningful = info.Exemption > 0m || info.Brackets.Any(b => b.Rate > 0m);
            if (!meaningful) continue;
            brackets = info.Brackets;
            exemption = info.Exemption;
            matched++;
            if (mi > 0) fromFallback++;
            if (exemption > 0) withExemption++;
            if (brackets.Count > 1) withBrackets++;
            break;
        }

        outList.Add(new OutComune(region, province, name, brackets, exemption));
    }

    outList = outList
        .OrderBy(x => x.Region, StringComparer.OrdinalIgnoreCase)
        .ThenBy(x => x.Province, StringComparer.OrdinalIgnoreCase)
        .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var path = Path.Combine(outDir, $"comuni-{year}.json");
    File.WriteAllText(path, JsonSerializer.Serialize(new OutFile(year, outList), outOpts), new UTF8Encoding(false));

    Console.WriteLine($"[{year}] scritto {path}");
    Console.WriteLine($"        comuni={outList.Count} matchMEF={matched} conEsenzione={withExemption} conScaglioni={withBrackets}");
    foreach (var probe in new[] { "Roma", "Milano", "Torino", "Abbadia Lariana", "Firenze" })
    {
        var m = outList.FirstOrDefault(x => x.Name.Equals(probe, StringComparison.OrdinalIgnoreCase));
        if (m is not null)
            Console.WriteLine($"        {probe} ({m.Province}/{m.Region}): scaglioni={m.Brackets.Count} top={m.Brackets[^1].Rate:P3} esenzione={m.ExemptionThreshold:N0}");
    }
}

Console.WriteLine("Fatto.");
return;

// ---------- parsing MEF ----------

Dictionary<string, (List<OutBracket> Brackets, decimal Exemption)> ParseMef(string path)
{
    var result = new Dictionary<string, (List<OutBracket>, decimal)>(StringComparer.OrdinalIgnoreCase);
    var lines = File.ReadAllLines(path, Encoding.UTF8);
    if (lines.Length == 0) return result;

    var header = lines[0].Split(';');
    int idxImporto = Array.FindIndex(header, h => h.Trim().Equals("IMPORTO_ESENTE", StringComparison.OrdinalIgnoreCase));
    var pairs = new List<(int A, int F)>();
    for (int i = 0; i < header.Length - 1; i++)
        if (header[i].Trim().StartsWith("ALIQUOTA", StringComparison.OrdinalIgnoreCase)
            && header[i + 1].Trim().StartsWith("FASCIA", StringComparison.OrdinalIgnoreCase))
            pairs.Add((i, i + 1));

    for (int r = 1; r < lines.Length; r++)
    {
        if (string.IsNullOrWhiteSpace(lines[r])) continue;
        var f = lines[r].Split(';');
        if (f.Length < 3) continue;
        var cc = f[0].Trim().ToUpperInvariant();
        if (cc.Length == 0) continue;

        var brackets = new List<OutBracket>();
        foreach (var (ai, fi) in pairs)
        {
            if (ai >= f.Length || fi >= f.Length) continue;
            var aRaw = f[ai].Trim();
            var fascia = f[fi].Trim();
            if (fascia.Length == 0 && aRaw.Length == 0) continue;
            if (fascia.Contains("senzione", StringComparison.OrdinalIgnoreCase)) continue; // riga di esenzione
            var rate = ParseRate(aRaw);
            if (rate is null) continue;
            brackets.Add(new OutBracket(ParseUpTo(fascia), rate.Value));
        }

        decimal exemption = (idxImporto >= 0 && idxImporto < f.Length ? ParseEuro(f[idxImporto]) : null) ?? 0m;

        if (brackets.Count == 0) brackets.Add(new OutBracket(Top, 0m));
        brackets = brackets.OrderBy(b => b.UpTo).ToList();
        // il rate più alto prosegue oltre l'ultimo scaglione
        brackets[^1] = brackets[^1] with { UpTo = Top };

        result[cc] = (brackets, exemption);
    }

    return result;
}

void GenerateRegions(int year)
{
    var path = Path.Combine(toolsDir, $"mef-reg-{year}.csv");
    if (!File.Exists(path)) { Console.WriteLine($"[{year}] mef-reg mancante: regioni non rigenerate"); return; }

    var map = RegionNameMap();
    var byRegion = new Dictionary<string, List<(decimal Up, decimal Rate)>>();
    var lines = File.ReadAllLines(path, Encoding.UTF8);
    for (int i = 1; i < lines.Length; i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i])) continue;
        var p = lines[i].Split(';');
        if (p.Length < 3) continue;
        var raw = p[1].Trim();
        var name = map.TryGetValue(raw, out var canon) ? canon : raw;
        var rate = ParseRate(p[^2].Trim());
        if (rate is null) continue;
        var up = ParseRegionalUpTo(p[^1].Trim());
        if (!byRegion.TryGetValue(name, out var list)) { list = new(); byRegion[name] = list; }
        list.Add((up, rate.Value));
    }

    var regioni = byRegion
        .Select(kv =>
        {
            var br = kv.Value
                .GroupBy(x => x.Up).Select(g => (Up: g.Key, Rate: g.Last().Rate))
                .OrderBy(x => x.Up)
                .Select(x => new OutBracket(x.Up, x.Rate)).ToList();
            if (br.Count == 0) br.Add(new OutBracket(Top, 0m));
            br[^1] = br[^1] with { UpTo = Top };
            return new OutRegion(kv.Key, br);
        })
        .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    var outPath = Path.Combine(outDir, $"regioni-{year}.json");
    File.WriteAllText(outPath, JsonSerializer.Serialize(new OutRegioniFile(year, regioni), outOpts), new UTF8Encoding(false));
    Console.WriteLine($"[{year}] scritto {outPath}  regioni={regioni.Count}");
}

decimal ParseRegionalUpTo(string fascia)
{
    var lower = fascia.ToLowerInvariant();
    if (lower.Contains("unica")) return Top;
    var nums = Regex.Matches(fascia, @"\d+(?:\.\d+)?")
        .Select(m => decimal.Parse(m.Value, CultureInfo.InvariantCulture))
        .ToList();
    if (lower.Contains("fino a") && nums.Count > 0) return nums[^1];
    if (lower.Contains("oltre")) return Top;
    return nums.Count > 0 ? nums[^1] : Top;
}

Dictionary<string, string> RegionNameMap() => new(StringComparer.OrdinalIgnoreCase)
{
    ["REGIONE ABRUZZO"] = "Abruzzo",
    ["REGIONE BASILICATA"] = "Basilicata",
    ["REGIONE CALABRIA"] = "Calabria",
    ["REGIONE CAMPANIA"] = "Campania",
    ["REGIONE EMILIA-ROMAGNA"] = "Emilia-Romagna",
    ["REGIONE FRIULI VENEZIA GIULIA"] = "Friuli-Venezia Giulia",
    ["REGIONE LAZIO"] = "Lazio",
    ["REGIONE LIGURIA"] = "Liguria",
    ["REGIONE LOMBARDIA"] = "Lombardia",
    ["REGIONE MARCHE"] = "Marche",
    ["REGIONE MOLISE"] = "Molise",
    ["REGIONE PIEMONTE"] = "Piemonte",
    ["REGIONE PUGLIA"] = "Puglia",
    ["REGIONE SARDEGNA"] = "Sardegna",
    ["REGIONE SICILIA"] = "Sicilia",
    ["REGIONE TOSCANA"] = "Toscana",
    ["REGIONE UMBRIA"] = "Umbria",
    ["REGIONE VALLE D'AOSTA"] = "Valle d'Aosta",
    ["REGIONE VENETO"] = "Veneto",
    ["PROVINCIA AUTONOMA DI TRENTO"] = "Provincia Autonoma di Trento",
    ["PROVINCIA AUTONOMA DI BOLZANO"] = "Provincia Autonoma di Bolzano",
};

decimal? ParseRate(string s)
{
    s = s.Trim();
    if (s.Length == 0) return null;
    s = s.Replace(",", ".");
    if (s.StartsWith(".")) s = "0" + s;
    return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
        ? (decimal)(v / 100.0)
        : null;
}

decimal ParseUpTo(string fascia)
{
    var lower = fascia.ToLowerInvariant();
    if (lower.Contains("unica") || lower.Contains("oltre")) return Top;

    var amounts = Regex.Matches(fascia, @"euro\s*([0-9][0-9\.]*(?:,[0-9]+)?)", RegexOptions.IgnoreCase)
        .Select(m => ParseEuro(m.Groups[1].Value))
        .Where(v => v is not null)
        .Select(v => v!.Value)
        .ToList();

    if (lower.Contains("fino a"))
        return lower.Contains(" da ") || lower.Contains("da euro")
            ? (amounts.Count > 0 ? amounts[^1] : Top)
            : (amounts.Count > 0 ? amounts[0] : Top);

    return amounts.Count > 0 ? amounts[^1] : Top;
}

decimal? ParseEuro(string s)
{
    s = s.Trim();
    if (s.Length == 0) return null;
    s = s.Replace(".", "").Replace(",", ".");
    return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;
}

string NormalizeRegion(string regione, string sigla)
{
    if (sigla.Equals("TN", StringComparison.OrdinalIgnoreCase)) return "Provincia Autonoma di Trento";
    if (sigla.Equals("BZ", StringComparison.OrdinalIgnoreCase)) return "Provincia Autonoma di Bolzano";
    if (regione.StartsWith("Valle d'Aosta", StringComparison.OrdinalIgnoreCase)) return "Valle d'Aosta";
    return regione;
}

string FindRepoRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "TaxManager.slnx")))
        dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("Repo root (TaxManager.slnx) non trovato.");
}

// ---------- modelli ----------

record RawComune(string? Nome, string? CodiceCatastale, string? Sigla, RawRegione? Regione, RawProvincia? Provincia);
record RawRegione(string? Nome);
record RawProvincia(string? Nome);

record OutBracket(decimal UpTo, decimal Rate);
record OutRegion(string Name, List<OutBracket> Brackets);
record OutRegioniFile(int Year, List<OutRegion> Regioni);
record OutComune(string Region, string Province, string Name, List<OutBracket> Brackets, decimal ExemptionThreshold);
record OutFile(int Year, List<OutComune> Comuni);
