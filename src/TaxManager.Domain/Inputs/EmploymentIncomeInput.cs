using TaxManager.Domain;

namespace TaxManager.Domain.Inputs;

/// <summary>Input del contribuente lavoratore dipendente (pubblico o privato).</summary>
public sealed record EmploymentIncomeInput
{
    /// <summary>Reddito Annuo Lordo (RAL), comprensivo della quota contributiva del lavoratore.</summary>
    public required decimal GrossAnnualSalary { get; init; }

    /// <summary>Settore (determina l'aliquota contributiva applicata).</summary>
    public EmploymentSector Sector { get; init; } = EmploymentSector.Privato;

    /// <summary>Giorni di lavoro nell'anno (per il ragguaglio di detrazioni e bonus).</summary>
    public int EmploymentDays { get; init; } = 365;

    /// <summary>
    /// Aliquota addizionale regionale FLAT (decimale). Usata solo come fallback quando
    /// <see cref="RegionalSurtaxSchedule"/> non è valorizzata.
    /// </summary>
    public decimal RegionalSurtaxRate { get; init; }

    /// <summary>
    /// Tariffa addizionale regionale a scaglioni, risolta dalla regione di residenza.
    /// Se valorizzata ha la precedenza su <see cref="RegionalSurtaxRate"/>.
    /// </summary>
    public TaxManager.Domain.Rules.ProgressiveSchedule? RegionalSurtaxSchedule { get; init; }

    /// <summary>Aliquota addizionale comunale FLAT (decimale). Usata come fallback (inserimento manuale).</summary>
    public decimal MunicipalSurtaxRate { get; init; }

    /// <summary>Tariffa addizionale comunale a scaglioni, risolta dal comune; precede <see cref="MunicipalSurtaxRate"/>.</summary>
    public TaxManager.Domain.Rules.ProgressiveSchedule? MunicipalSurtaxSchedule { get; init; }

    /// <summary>
    /// Soglia di esenzione dell'addizionale comunale: se l'imponibile è ≤ soglia,
    /// l'addizionale comunale non è dovuta (0 = nessuna esenzione).
    /// </summary>
    public decimal MunicipalExemptionThreshold { get; init; }

    /// <summary>Oneri deducibili che abbattono l'imponibile (es. previdenza complementare).</summary>
    public decimal DeductibleCharges { get; init; }

    /// <summary>Altre detrazioni d'imposta (oneri al 19%, ecc.) — semplificato a un importo unico (importo della detrazione già calcolata).</summary>
    public decimal OtherTaxCredits { get; init; }

    /// <summary>Contributi a previdenza complementare (fondi pensione/PIP): deducibili dal reddito entro il tetto annuo.</summary>
    public decimal ComplementaryPensionContribution { get; init; }
}
