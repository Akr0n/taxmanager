using TaxManager.Domain.Rules;

namespace TaxManager.Application.Abstractions;

/// <summary>Fornisce i ruleset normativi per anno fiscale (da JSON su disco, con fallback in codice).</summary>
public interface IRulesetProvider
{
    /// <summary>Anni fiscali disponibili, in ordine crescente.</summary>
    IReadOnlyList<int> AvailableYears { get; }

    /// <summary>Restituisce il ruleset dell'anno indicato.</summary>
    TaxYearRuleset Get(int year);
}
