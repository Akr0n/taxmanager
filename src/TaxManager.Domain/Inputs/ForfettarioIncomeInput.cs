using TaxManager.Domain;

namespace TaxManager.Domain.Inputs;

/// <summary>Input del contribuente in regime forfettario.</summary>
public sealed record ForfettarioIncomeInput
{
    /// <summary>Ricavi o compensi incassati nell'anno.</summary>
    public required decimal Revenue { get; init; }

    /// <summary>Coefficiente di redditività del settore ATECO (decimale 0..1, es. 0,78).</summary>
    public required decimal Coefficient { get; init; }

    /// <summary>True se si applica l'aliquota ridotta start-up (5%).</summary>
    public bool IsStartup { get; init; }

    /// <summary>Gestione previdenziale.</summary>
    public ForfettarioContributionScheme Scheme { get; init; } = ForfettarioContributionScheme.GestioneSeparata;

    /// <summary>Applica la riduzione contributiva (es. 35%) per artigiani/commercianti forfettari.</summary>
    public bool ApplyArtigianiCommerciantiReduction { get; init; }

    /// <summary>Aliquota della cassa professionale, usata solo se <see cref="Scheme"/> = CassaProfessionale.</summary>
    public decimal CassaRate { get; init; }

    /// <summary>
    /// Contributi previdenziali effettivamente versati nell'anno e quindi deducibili.
    /// Se null, si deduce la stima dei contributi dovuti calcolata dall'engine.
    /// </summary>
    public decimal? PaidContributionsDeductibleOverride { get; init; }
}
