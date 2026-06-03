using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaxManager.Application.Abstractions;
using TaxManager.Domain;
using TaxManager.Domain.Results;

namespace TaxManager.Infrastructure.Persistence;

/// <summary>Esporta il prospetto di calcolo su disco come testo formattato o JSON.</summary>
public sealed class FileReportExporter : IReportExporter
{
    private static readonly CultureInfo It = CultureInfo.GetCultureInfo("it-IT");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task ExportTextAsync(TaxResult result, string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine("  TaxManager — Prospetto di calcolo imposte");
        sb.AppendLine("══════════════════════════════════════════════════════");
        sb.AppendLine($"Categoria : {Describe(result.Category)}");
        sb.AppendLine($"Anno      : {result.Year}");
        sb.AppendLine($"Generato  : {DateTime.Now.ToString("g", It)}");
        sb.AppendLine(new string('-', 54));

        foreach (var line in result.Lines)
            sb.AppendLine($"{Truncate(line.Label, 40),-40} {Currency(line.Amount),12}");

        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"{"Pressione fiscale (imposte/lordo)",-40} {result.EffectiveTaxRate.ToString("P2", It),12}");
        sb.AppendLine($"{"Carico totale (imposte+contributi)",-40} {result.TotalBurdenRate.ToString("P2", It),12}");

        if (result.Notes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Note:");
            foreach (var note in result.Notes)
                sb.AppendLine($" • {note}");
        }

        sb.AppendLine();
        sb.AppendLine("I valori sono una STIMA. Verificare sempre con le fonti ufficiali e un professionista.");

        EnsureDir(filePath);
        await File.WriteAllTextAsync(filePath, sb.ToString(), new UTF8Encoding(false), ct);
    }

    public async Task ExportJsonAsync(TaxResult result, string filePath, CancellationToken ct = default)
    {
        EnsureDir(filePath);
        await using var fs = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fs, result, JsonOptions, ct);
    }

    private static string Currency(decimal value) => value.ToString("C2", It);

    private static string Describe(TaxpayerCategory c) => c switch
    {
        TaxpayerCategory.DipendentePrivato => "Lavoratore dipendente (privato)",
        TaxpayerCategory.DipendentePubblico => "Lavoratore dipendente (pubblico)",
        TaxpayerCategory.Forfettario => "Partita IVA forfettaria",
        _ => c.ToString(),
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    private static void EnsureDir(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }
}
