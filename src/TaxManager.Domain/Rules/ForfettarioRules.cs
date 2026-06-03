namespace TaxManager.Domain.Rules;

/// <summary>Coefficiente di redditività associato a un gruppo di settore ATECO.</summary>
public sealed record ForfettarioCoefficient(string Sector, string AtecoHint, decimal Coefficient);

/// <summary>
/// Regola contributiva a minimale (gestioni artigiani / commercianti INPS):
/// contributo fisso sul minimale + percentuale sulla parte eccedente; opzionale riduzione (es. 35%) per i forfettari.
/// </summary>
public sealed record ContributionBandRule(
    decimal Minimale,
    decimal FixedContribution,
    decimal RateAboveMinimale,
    decimal ReductionForfettario)
{
    public decimal Compute(decimal redditoForfettario, bool applyReduction)
    {
        if (redditoForfettario <= 0) return 0m;
        var variable = redditoForfettario > Minimale
            ? (redditoForfettario - Minimale) * RateAboveMinimale
            : 0m;
        var total = FixedContribution + variable;
        if (applyReduction && ReductionForfettario > 0)
            total *= 1m - ReductionForfettario;
        return total;
    }
}

/// <summary>Parametri del regime forfettario per un dato anno.</summary>
public sealed record ForfettarioRuleset
{
    /// <summary>Limite di ricavi/compensi per l'accesso/permanenza nel regime (es. €85.000).</summary>
    public required decimal RevenueLimit { get; init; }

    /// <summary>Aliquota dell'imposta sostitutiva standard (es. 15%).</summary>
    public required decimal SubstituteTaxRate { get; init; }

    /// <summary>Aliquota ridotta per le nuove attività (start-up, es. 5%).</summary>
    public required decimal StartupReducedRate { get; init; }

    /// <summary>Numero di anni in cui si applica l'aliquota ridotta start-up.</summary>
    public required int StartupYears { get; init; }

    /// <summary>Tabella dei coefficienti di redditività per gruppo ATECO.</summary>
    public required IReadOnlyList<ForfettarioCoefficient> Coefficients { get; init; }

    /// <summary>Aliquota Gestione Separata INPS (professionisti senza cassa).</summary>
    public required decimal GestioneSeparataRate { get; init; }

    /// <summary>Regola contributiva gestione artigiani.</summary>
    public required ContributionBandRule Artigiani { get; init; }

    /// <summary>Regola contributiva gestione commercianti.</summary>
    public required ContributionBandRule Commercianti { get; init; }
}
