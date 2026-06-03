using TaxManager.Domain;

namespace TaxManager.Domain.Results;

/// <summary>Natura di una voce del prospetto di calcolo (per la presentazione/raggruppamento).</summary>
public enum TaxLineKind
{
    Reddito,
    Contributo,
    BaseImponibile,
    Imposta,
    Detrazione,
    Credito,
    Addizionale,
    Subtotale,
    Totale,
    Netto,
    Informativa
}

/// <summary>Una voce del prospetto auditabile. Gli importi sono in valore assoluto positivo;
/// la natura (<see cref="Kind"/>) ne indica il segno logico.</summary>
public sealed record TaxLineItem(string Code, string Label, decimal Amount, TaxLineKind Kind);

/// <summary>
/// Esito del calcolo: una scomposizione passo-passo (auditabile) più i totali sintetici.
/// </summary>
public sealed record TaxResult
{
    public required TaxpayerCategory Category { get; init; }
    public required int Year { get; init; }

    /// <summary>Reddito di partenza: RAL (dipendente) o ricavi (forfettario).</summary>
    public required decimal GrossIncome { get; init; }

    /// <summary>Contributi previdenziali totali.</summary>
    public required decimal SocialContributions { get; init; }

    /// <summary>Base imponibile dell'imposta principale.</summary>
    public required decimal TaxableIncome { get; init; }

    /// <summary>Imposta lorda (IRPEF lorda o imposta sostitutiva lorda).</summary>
    public required decimal GrossTax { get; init; }

    /// <summary>Detrazioni d'imposta complessive.</summary>
    public decimal Deductions { get; init; }

    /// <summary>Imposta netta principale (IRPEF netta o imposta sostitutiva).</summary>
    public required decimal NetTax { get; init; }

    public decimal RegionalSurtax { get; init; }
    public decimal MunicipalSurtax { get; init; }

    /// <summary>Trattamento integrativo / bonus (riduce il carico fiscale).</summary>
    public decimal TreatmentBonus { get; init; }

    /// <summary>Imposte totali dovute (al netto del bonus).</summary>
    public required decimal TotalTaxes { get; init; }

    /// <summary>Reddito netto annuo (in tasca).</summary>
    public required decimal NetAnnualIncome { get; init; }

    /// <summary>Pressione fiscale: imposte / reddito lordo.</summary>
    public required decimal EffectiveTaxRate { get; init; }

    /// <summary>Carico totale: (imposte + contributi) / reddito lordo.</summary>
    public required decimal TotalBurdenRate { get; init; }

    /// <summary>Prospetto dettagliato.</summary>
    public required IReadOnlyList<TaxLineItem> Lines { get; init; }

    /// <summary>Note/avvisi (es. superamento soglie, semplificazioni adottate).</summary>
    public IReadOnlyList<string> Notes { get; init; } = Array.Empty<string>();
}
