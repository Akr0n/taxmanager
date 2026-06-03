namespace TaxManager.Domain.Rules;

/// <summary>
/// La "fotografia" normativa di un anno fiscale: tutte le regole/aliquote/soglie come DATI versionati.
/// Caricabile da JSON (vedi <c>data/rulesets/&lt;anno&gt;.json</c>). I metodi di calcolo vivono sui tipi,
/// quindi un ruleset deserializzato conserva la propria logica.
/// </summary>
public sealed record TaxYearRuleset
{
    public required int Year { get; init; }

    /// <summary>Scaglioni IRPEF nazionali.</summary>
    public required ProgressiveSchedule Irpef { get; init; }

    /// <summary>Detrazione per redditi da lavoro dipendente.</summary>
    public required EmploymentDeductionRule EmploymentDeduction { get; init; }

    /// <summary>Trattamento integrativo / bonus (facoltativo).</summary>
    public TreatmentBonusRule? TreatmentBonus { get; init; }

    /// <summary>Somma integrativa del cuneo fiscale (introdotta dal 2025) — facoltativa.</summary>
    public IntegrativeAllowanceRule? IntegrativeAllowance { get; init; }

    /// <summary>Ulteriore detrazione per redditi medi (dal 2025) — facoltativa.</summary>
    public AdditionalDeductionRule? AdditionalDeduction { get; init; }

    /// <summary>Quota contributiva a carico del lavoratore dipendente privato.</summary>
    public required EmployeeContributionRule PrivateEmployeeContribution { get; init; }

    /// <summary>Quota contributiva a carico del lavoratore dipendente pubblico.</summary>
    public required EmployeeContributionRule PublicEmployeeContribution { get; init; }

    /// <summary>Aliquota addizionale regionale "tipica" usata come default (l'utente può sovrascriverla).</summary>
    public decimal DefaultRegionalSurtaxRate { get; init; }

    /// <summary>Aliquota addizionale comunale "tipica" usata come default.</summary>
    public decimal DefaultMunicipalSurtaxRate { get; init; }

    /// <summary>Tetto annuo deducibile per la previdenza complementare (art. 8 D.Lgs. 252/2005).</summary>
    public decimal ComplementaryPensionCap { get; init; } = 5164.57m;

    /// <summary>Parametri del regime forfettario.</summary>
    public required ForfettarioRuleset Forfettario { get; init; }
}
