using TaxManager.Domain.Rules;

namespace TaxManager.Infrastructure.Rulesets;

// DTO che rispecchiano la forma dei file JSON in data/rulesets.
// Vengono mappati sui tipi del dominio da RulesetMapper.

internal sealed class RulesetDto
{
    public int Year { get; set; }
    public ScheduleDto? Irpef { get; set; }
    public EmploymentDeductionDto? EmploymentDeduction { get; set; }
    public TreatmentBonusDto? TreatmentBonus { get; set; }
    public IntegrativeAllowanceDto? IntegrativeAllowance { get; set; }
    public AdditionalDeductionDto? AdditionalDeduction { get; set; }
    public EmployeeContributionsDto? EmployeeContributions { get; set; }
    public SurtaxDto? RegionalSurtax { get; set; }
    public SurtaxDto? MunicipalSurtax { get; set; }
    public ForfettarioDto? Forfettario { get; set; }
}

internal sealed class ScheduleDto
{
    public List<BracketDto> Brackets { get; set; } = new();
}

internal sealed class BracketDto
{
    public decimal UpTo { get; set; }
    public decimal Rate { get; set; }
}

internal sealed class EmploymentDeductionDto
{
    public List<DeductionBandDto> Bands { get; set; } = new();
    public decimal MinAmount { get; set; }
}

internal sealed class DeductionBandDto
{
    public decimal IncomeUpTo { get; set; }
    public decimal Base { get; set; }
    public decimal Taper { get; set; }
    public decimal TaperFrom { get; set; }
    public decimal TaperTo { get; set; }
}

internal sealed class TreatmentBonusDto
{
    public decimal IncomeThreshold { get; set; }
    public decimal AnnualAmount { get; set; }
}

internal sealed class IntegrativeAllowanceDto
{
    public decimal IncomeCeiling { get; set; }
    public List<BonusBandDto> Bands { get; set; } = new();
}

internal sealed class BonusBandDto
{
    public decimal IncomeUpTo { get; set; }
    public decimal Rate { get; set; }
}

internal sealed class AdditionalDeductionDto
{
    public decimal FromIncome { get; set; }
    public decimal FlatUpTo { get; set; }
    public decimal FlatAmount { get; set; }
    public decimal TaperTo { get; set; }
}

internal sealed class EmployeeContributionsDto
{
    public ContributionDto? Private { get; set; }
    public ContributionDto? Public { get; set; }
}

internal sealed class ContributionDto
{
    public decimal Rate { get; set; }
    public decimal AdditionalRate { get; set; }
    public decimal AdditionalThreshold { get; set; }
    public decimal AnnualCeiling { get; set; }
}

internal sealed class SurtaxDto
{
    public decimal DefaultRate { get; set; }
    public decimal MinRate { get; set; }
    public decimal MaxRate { get; set; }
}

internal sealed class ForfettarioDto
{
    public decimal RevenueLimit { get; set; }
    public decimal SubstituteTaxRate { get; set; }
    public decimal StartupReducedRate { get; set; }
    public int StartupYears { get; set; }
    public List<CoefficientDto> Coefficients { get; set; } = new();
    public ForfettarioContributionsDto? Contributions { get; set; }
}

internal sealed class CoefficientDto
{
    public string Sector { get; set; } = string.Empty;
    public string AtecoHint { get; set; } = string.Empty;
    public decimal Coefficient { get; set; }
}

internal sealed class ForfettarioContributionsDto
{
    public decimal GestioneSeparataRate { get; set; }
    public ContributionBandDto? Artigiani { get; set; }
    public ContributionBandDto? Commercianti { get; set; }
}

internal sealed class ContributionBandDto
{
    public decimal Minimale { get; set; }
    public decimal FixedContribution { get; set; }
    public decimal RateAboveMinimale { get; set; }
    public decimal ReductionForfettario { get; set; }
}
