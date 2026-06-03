using System.Text.Json;
using TaxManager.Application.Abstractions;
using TaxManager.Domain;
using TaxManager.Domain.Rules;

namespace TaxManager.Infrastructure.Rulesets;

/// <summary>
/// Fornisce i ruleset combinando il fallback in codice (<see cref="SampleRulesets"/>) con gli
/// eventuali file JSON presenti su disco, che hanno la precedenza. I file malformati vengono
/// ignorati mantenendo il fallback, così l'app resta sempre funzionante.
/// </summary>
public sealed class JsonRulesetProvider : IRulesetProvider
{
    private readonly Dictionary<int, TaxYearRuleset> _byYear = new();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public JsonRulesetProvider(string rulesetsDirectory)
    {
        // 1) Fallback in codice (sempre disponibile).
        foreach (var year in SampleRulesets.AvailableYears)
            _byYear[year] = SampleRulesets.For(year);

        // 2) Override da disco.
        if (Directory.Exists(rulesetsDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(rulesetsDirectory, "*.json"))
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<RulesetDto>(File.ReadAllText(file), JsonOptions);
                    if (dto is null) continue;
                    var ruleset = RulesetMapper.Map(dto);
                    _byYear[ruleset.Year] = ruleset;
                }
                catch
                {
                    // File malformato: si mantiene il ruleset di fallback per quell'anno.
                }
            }
        }

        AvailableYears = _byYear.Keys.OrderBy(y => y).ToArray();
    }

    public IReadOnlyList<int> AvailableYears { get; }

    public TaxYearRuleset Get(int year) =>
        _byYear.TryGetValue(year, out var ruleset)
            ? ruleset
            : throw new ArgumentOutOfRangeException(nameof(year), year, "Ruleset non disponibile per l'anno richiesto.");
}
