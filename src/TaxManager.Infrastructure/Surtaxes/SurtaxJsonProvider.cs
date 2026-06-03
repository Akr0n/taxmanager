using System.Text.Json;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;
using TaxManager.Domain.Rules;

namespace TaxManager.Infrastructure.Surtaxes;

/// <summary>
/// Fornisce le addizionali da <c>data/addizionali</c>: <c>regioni-&lt;anno&gt;.json</c> e
/// <c>comuni-&lt;anno&gt;.json</c> (tutti i comuni italiani). I dati sono caricati e messi in cache
/// per anno; file mancanti/malformati → liste vuote (degradazione sicura).
/// </summary>
public sealed class SurtaxJsonProvider : ISurtaxProvider
{
    private readonly string _dir;
    private readonly object _lock = new();
    private readonly Dictionary<int, IReadOnlyList<RegionSurtax>> _regionsCache = new();
    private readonly Dictionary<int, IReadOnlyList<MunicipalitySurtax>> _municipalitiesCache = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public SurtaxJsonProvider(string addizionaliDirectory) => _dir = addizionaliDirectory;

    public IReadOnlyList<RegionSurtax> GetRegions(int year)
    {
        lock (_lock)
        {
            if (_regionsCache.TryGetValue(year, out var cached)) return cached;
            var list = LoadRegions(year);
            _regionsCache[year] = list;
            return list;
        }
    }

    public IReadOnlyList<string> GetProvinces(int year, string region) =>
        Municipalities(year)
            .Where(m => string.Equals(m.Region, region, StringComparison.CurrentCultureIgnoreCase))
            .Select(m => m.Province)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public IReadOnlyList<MunicipalitySurtax> GetMunicipalities(int year, string region, string province) =>
        Municipalities(year)
            .Where(m => string.Equals(m.Region, region, StringComparison.CurrentCultureIgnoreCase)
                     && string.Equals(m.Province, province, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private IReadOnlyList<MunicipalitySurtax> Municipalities(int year)
    {
        lock (_lock)
        {
            if (_municipalitiesCache.TryGetValue(year, out var cached)) return cached;
            var list = LoadMunicipalities(year);
            _municipalitiesCache[year] = list;
            return list;
        }
    }

    private IReadOnlyList<RegionSurtax> LoadRegions(int year)
    {
        var path = Path.Combine(_dir, $"regioni-{year}.json");
        if (!File.Exists(path)) return Array.Empty<RegionSurtax>();
        try
        {
            var dto = JsonSerializer.Deserialize<RegioniFileDto>(File.ReadAllText(path), JsonOptions);
            if (dto?.Regioni is null) return Array.Empty<RegionSurtax>();
            return dto.Regioni
                .Select(r => new RegionSurtax
                {
                    Region = r.Name,
                    Schedule = new ProgressiveSchedule(
                        r.Brackets.Select(b => new ProgressiveBracket(b.UpTo, b.Rate)).ToList()),
                })
                .OrderBy(r => r.Region, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<RegionSurtax>();
        }
    }

    private IReadOnlyList<MunicipalitySurtax> LoadMunicipalities(int year)
    {
        var path = Path.Combine(_dir, $"comuni-{year}.json");
        if (!File.Exists(path)) return Array.Empty<MunicipalitySurtax>();
        try
        {
            var dto = JsonSerializer.Deserialize<ComuniFileDto>(File.ReadAllText(path), JsonOptions);
            if (dto?.Comuni is null) return Array.Empty<MunicipalitySurtax>();
            return dto.Comuni
                .Select(c => new MunicipalitySurtax
                {
                    Region = c.Region,
                    Province = c.Province,
                    Name = c.Name,
                    Schedule = new ProgressiveSchedule(
                        c.Brackets.Select(b => new ProgressiveBracket(b.UpTo, b.Rate)).ToList()),
                    ExemptionThreshold = c.ExemptionThreshold,
                })
                .ToList();
        }
        catch
        {
            return Array.Empty<MunicipalitySurtax>();
        }
    }
}
