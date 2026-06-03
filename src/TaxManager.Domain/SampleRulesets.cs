using TaxManager.Domain.Rules;

namespace TaxManager.Domain;

/// <summary>
/// Ruleset codificati in C# come sorgente di riferimento e fallback (usati dai test e quando
/// i file JSON in <c>data/rulesets</c> non sono disponibili).
/// <para>
/// I valori derivano da ricerca verificata sulle norme 2024/2025 (Agenzia delle Entrate, INPS,
/// Legge di Bilancio). Restano una STIMA: vanno confermati sulle fonti ufficiali per ogni anno.
/// </para>
/// </summary>
public static class SampleRulesets
{
    /// <summary>Anni fiscali per cui esiste un ruleset di riferimento.</summary>
    public static IReadOnlyList<int> AvailableYears { get; } = new[] { 2024, 2025, 2026 };

    public static TaxYearRuleset For(int year) => year switch
    {
        2024 => Year2024(),
        2025 => Year2025(),
        2026 => Year2026(),
        _ => throw new ArgumentOutOfRangeException(nameof(year), year, "Nessun ruleset di riferimento per l'anno richiesto.")
    };

    private static ProgressiveSchedule Irpef() => new(new ProgressiveBracket[]
    {
        new(UpTo: 28_000m, Rate: 0.23m),
        new(UpTo: 50_000m, Rate: 0.35m),
        new(UpTo: decimal.MaxValue, Rate: 0.43m),
    });

    private static EmploymentDeductionRule EmploymentDeduction() => new(
        Bands: new EmploymentDeductionBand[]
        {
            new(IncomeUpTo: 15_000m, Base: 1_955m, Taper: 0m,     TaperFrom: 0m,      TaperTo: 0m),
            new(IncomeUpTo: 28_000m, Base: 1_910m, Taper: 1_190m, TaperFrom: 15_000m, TaperTo: 28_000m),
            new(IncomeUpTo: 50_000m, Base: 0m,     Taper: 1_910m, TaperFrom: 28_000m, TaperTo: 50_000m),
        },
        MinAmount: 690m);

    private static IReadOnlyList<ForfettarioCoefficient> Coefficients() => new ForfettarioCoefficient[]
    {
        new("Industrie alimentari e delle bevande", "10-11", 0.40m),
        new("Commercio all'ingrosso e al dettaglio", "45 - 46.2/46.9 - 47.1/47.7 - 47.9", 0.40m),
        new("Commercio ambulante di prodotti alimentari e bevande", "47.81", 0.40m),
        new("Commercio ambulante di altri prodotti", "47.82 - 47.89", 0.54m),
        new("Costruzioni e attività immobiliari", "41-42-43 - 68", 0.86m),
        new("Intermediari del commercio", "46.1", 0.62m),
        new("Servizi di alloggio e ristorazione", "55-56", 0.40m),
        new("Attività professionali, scientifiche, tecniche, sanitarie, istruzione, servizi finanziari/assicurativi", "64-66 - 69-75 - 85 - 86-88", 0.78m),
        new("Altre attività economiche", "altri codici ATECO", 0.67m),
    };

    public static TaxYearRuleset Year2024() => new()
    {
        Year = 2024,
        Irpef = Irpef(),
        EmploymentDeduction = EmploymentDeduction(),
        TreatmentBonus = new TreatmentBonusRule(IncomeThreshold: 15_000m, AnnualAmount: 1_200m),
        // Cuneo 2025 non presente nel 2024 (al suo posto vigeva l'esonero contributivo).
        IntegrativeAllowance = null,
        AdditionalDeduction = null,
        PrivateEmployeeContribution = new EmployeeContributionRule(Rate: 0.0919m, AdditionalRate: 0.01m, AdditionalThreshold: 55_008m, AnnualCeiling: 119_650m),
        PublicEmployeeContribution = new EmployeeContributionRule(Rate: 0.0880m, AdditionalRate: 0.01m, AdditionalThreshold: 55_008m, AnnualCeiling: 119_650m),
        DefaultRegionalSurtaxRate = 0.0123m,
        DefaultMunicipalSurtaxRate = 0.0060m,
        Forfettario = new ForfettarioRuleset
        {
            RevenueLimit = 85_000m,
            SubstituteTaxRate = 0.15m,
            StartupReducedRate = 0.05m,
            StartupYears = 5,
            Coefficients = Coefficients(),
            GestioneSeparataRate = 0.2607m,
            Artigiani = new ContributionBandRule(Minimale: 18_415m, FixedContribution: 4_427.04m, RateAboveMinimale: 0.25m, ReductionForfettario: 0.35m),
            Commercianti = new ContributionBandRule(Minimale: 18_415m, FixedContribution: 4_515.43m, RateAboveMinimale: 0.2548m, ReductionForfettario: 0.35m),
        },
    };

    public static TaxYearRuleset Year2025() => new()
    {
        Year = 2025,
        Irpef = Irpef(),
        EmploymentDeduction = EmploymentDeduction(),
        TreatmentBonus = new TreatmentBonusRule(IncomeThreshold: 15_000m, AnnualAmount: 1_200m),
        // Cuneo fiscale 2025 (L. 207/2024): somma integrativa + ulteriore detrazione.
        IntegrativeAllowance = new IntegrativeAllowanceRule(
            IncomeCeiling: 20_000m,
            Bands: new EmploymentBonusBand[]
            {
                new(IncomeUpTo: 8_500m,  Rate: 0.071m),
                new(IncomeUpTo: 15_000m, Rate: 0.053m),
                new(IncomeUpTo: 20_000m, Rate: 0.048m),
            }),
        AdditionalDeduction = new AdditionalDeductionRule(FromIncome: 20_000m, FlatUpTo: 32_000m, FlatAmount: 1_000m, TaperTo: 40_000m),
        PrivateEmployeeContribution = new EmployeeContributionRule(Rate: 0.0919m, AdditionalRate: 0.01m, AdditionalThreshold: 55_448m, AnnualCeiling: 120_607m),
        PublicEmployeeContribution = new EmployeeContributionRule(Rate: 0.0880m, AdditionalRate: 0.01m, AdditionalThreshold: 55_448m, AnnualCeiling: 120_607m),
        DefaultRegionalSurtaxRate = 0.0123m,
        DefaultMunicipalSurtaxRate = 0.0060m,
        Forfettario = new ForfettarioRuleset
        {
            RevenueLimit = 85_000m,
            SubstituteTaxRate = 0.15m,
            StartupReducedRate = 0.05m,
            StartupYears = 5,
            Coefficients = Coefficients(),
            GestioneSeparataRate = 0.2607m,
            Artigiani = new ContributionBandRule(Minimale: 18_555m, FixedContribution: 4_460.64m, RateAboveMinimale: 0.25m, ReductionForfettario: 0.35m),
            Commercianti = new ContributionBandRule(Minimale: 18_555m, FixedContribution: 4_549.70m, RateAboveMinimale: 0.2548m, ReductionForfettario: 0.35m),
        },
    };

    public static TaxYearRuleset Year2026() => new()
    {
        Year = 2026,
        // Legge di Bilancio 2026 (L. 199/2025): aliquota IRPEF centrale ridotta dal 35% al 33%.
        Irpef = new ProgressiveSchedule(new ProgressiveBracket[]
        {
            new(UpTo: 28_000m,          Rate: 0.23m),
            new(UpTo: 50_000m,          Rate: 0.33m),
            new(UpTo: decimal.MaxValue, Rate: 0.43m),
        }),
        EmploymentDeduction = EmploymentDeduction(),
        TreatmentBonus = new TreatmentBonusRule(IncomeThreshold: 15_000m, AnnualAmount: 1_200m),
        IntegrativeAllowance = new IntegrativeAllowanceRule(
            IncomeCeiling: 20_000m,
            Bands: new EmploymentBonusBand[]
            {
                new(IncomeUpTo: 8_500m,  Rate: 0.071m),
                new(IncomeUpTo: 15_000m, Rate: 0.053m),
                new(IncomeUpTo: 20_000m, Rate: 0.048m),
            }),
        AdditionalDeduction = new AdditionalDeductionRule(FromIncome: 20_000m, FlatUpTo: 32_000m, FlatAmount: 1_000m, TaperTo: 40_000m),
        PrivateEmployeeContribution = new EmployeeContributionRule(Rate: 0.0919m, AdditionalRate: 0.01m, AdditionalThreshold: 56_224m, AnnualCeiling: 122_295m),
        PublicEmployeeContribution = new EmployeeContributionRule(Rate: 0.0880m, AdditionalRate: 0.01m, AdditionalThreshold: 56_224m, AnnualCeiling: 122_295m),
        DefaultRegionalSurtaxRate = 0.0123m,
        DefaultMunicipalSurtaxRate = 0.0080m,
        Forfettario = new ForfettarioRuleset
        {
            RevenueLimit = 85_000m,
            SubstituteTaxRate = 0.15m,
            StartupReducedRate = 0.05m,
            StartupYears = 5,
            Coefficients = Coefficients(),
            GestioneSeparataRate = 0.2607m,
            Artigiani = new ContributionBandRule(Minimale: 18_808m, FixedContribution: 4_521.36m, RateAboveMinimale: 0.24m, ReductionForfettario: 0.35m),
            Commercianti = new ContributionBandRule(Minimale: 18_808m, FixedContribution: 4_611.64m, RateAboveMinimale: 0.2448m, ReductionForfettario: 0.35m),
        },
    };
}
