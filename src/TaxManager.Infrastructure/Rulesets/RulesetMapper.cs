using TaxManager.Domain.Rules;

namespace TaxManager.Infrastructure.Rulesets;

/// <summary>Mappa i DTO JSON sui tipi (record) del dominio.</summary>
internal static class RulesetMapper
{
    public static TaxYearRuleset Map(RulesetDto d)
    {
        if (d.Irpef is null || d.EmploymentDeduction is null
            || d.EmployeeContributions?.Private is null || d.EmployeeContributions?.Public is null
            || d.Forfettario is null)
        {
            throw new InvalidDataException($"Ruleset {d.Year}: sezioni obbligatorie mancanti.");
        }

        return new TaxYearRuleset
        {
            Year = d.Year,
            Irpef = new ProgressiveSchedule(
                d.Irpef.Brackets.Select(b => new ProgressiveBracket(b.UpTo, b.Rate)).ToList()),
            EmploymentDeduction = new EmploymentDeductionRule(
                d.EmploymentDeduction.Bands
                    .Select(x => new EmploymentDeductionBand(x.IncomeUpTo, x.Base, x.Taper, x.TaperFrom, x.TaperTo))
                    .ToList(),
                d.EmploymentDeduction.MinAmount),
            TreatmentBonus = d.TreatmentBonus is null
                ? null
                : new TreatmentBonusRule(d.TreatmentBonus.IncomeThreshold, d.TreatmentBonus.AnnualAmount),
            IntegrativeAllowance = d.IntegrativeAllowance is null
                ? null
                : new IntegrativeAllowanceRule(
                    d.IntegrativeAllowance.IncomeCeiling,
                    d.IntegrativeAllowance.Bands.Select(x => new EmploymentBonusBand(x.IncomeUpTo, x.Rate)).ToList()),
            AdditionalDeduction = d.AdditionalDeduction is null
                ? null
                : new AdditionalDeductionRule(
                    d.AdditionalDeduction.FromIncome, d.AdditionalDeduction.FlatUpTo,
                    d.AdditionalDeduction.FlatAmount, d.AdditionalDeduction.TaperTo),
            PrivateEmployeeContribution = MapContribution(d.EmployeeContributions.Private),
            PublicEmployeeContribution = MapContribution(d.EmployeeContributions.Public),
            DefaultRegionalSurtaxRate = d.RegionalSurtax?.DefaultRate ?? 0m,
            DefaultMunicipalSurtaxRate = d.MunicipalSurtax?.DefaultRate ?? 0m,
            Forfettario = MapForfettario(d.Forfettario),
        };
    }

    private static EmployeeContributionRule MapContribution(ContributionDto c) =>
        new(c.Rate, c.AdditionalRate, c.AdditionalThreshold, c.AnnualCeiling);

    private static ForfettarioRuleset MapForfettario(ForfettarioDto f)
    {
        var contributions = f.Contributions
            ?? throw new InvalidDataException("Forfettario: sezione 'contributions' mancante.");
        var artigiani = contributions.Artigiani
            ?? throw new InvalidDataException("Forfettario: 'artigiani' mancante.");
        var commercianti = contributions.Commercianti
            ?? throw new InvalidDataException("Forfettario: 'commercianti' mancante.");

        return new ForfettarioRuleset
        {
            RevenueLimit = f.RevenueLimit,
            SubstituteTaxRate = f.SubstituteTaxRate,
            StartupReducedRate = f.StartupReducedRate,
            StartupYears = f.StartupYears,
            Coefficients = f.Coefficients
                .Select(c => new ForfettarioCoefficient(c.Sector, c.AtecoHint, c.Coefficient))
                .ToList(),
            GestioneSeparataRate = contributions.GestioneSeparataRate,
            Artigiani = new ContributionBandRule(artigiani.Minimale, artigiani.FixedContribution, artigiani.RateAboveMinimale, artigiani.ReductionForfettario),
            Commercianti = new ContributionBandRule(commercianti.Minimale, commercianti.FixedContribution, commercianti.RateAboveMinimale, commercianti.ReductionForfettario),
        };
    }
}
