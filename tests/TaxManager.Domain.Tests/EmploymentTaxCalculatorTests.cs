using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;

namespace TaxManager.Domain.Tests;

public class EmploymentTaxCalculatorTests
{
    private readonly EmploymentTaxCalculator _calc = new();

    [Fact]
    public void Private_employee_2024_full_breakdown()
    {
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 30_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxRate = 0.0123m,
            MunicipalSurtaxRate = 0.0060m,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.Equal(TaxpayerCategory.DipendentePrivato, r.Category);
        Assert.Equal(2757.00m, r.SocialContributions);   // 30.000 * 9,19%
        Assert.Equal(27243m, r.TaxableIncome);           // RAL - contributi
        Assert.Equal(4287m, r.NetTax);                   // IRPEF netta (arrotondata)
        Assert.Equal(335m, r.RegionalSurtax);
        Assert.Equal(163m, r.MunicipalSurtax);
        Assert.Equal(4785m, r.TotalTaxes);
        Assert.Equal(22458m, r.NetAnnualIncome);
    }

    [Fact]
    public void Public_employee_pays_lower_contribution_rate_than_private()
    {
        var input = new EmploymentIncomeInput { GrossAnnualSalary = 40_000m, EmploymentDays = 365 };

        var priv = _calc.Calculate(input with { Sector = EmploymentSector.Privato }, SampleRulesets.Year2025());
        var pub = _calc.Calculate(input with { Sector = EmploymentSector.Pubblico }, SampleRulesets.Year2025());

        // 8,80% pubblico < 9,19% privato => meno contributi.
        Assert.True(pub.SocialContributions < priv.SocialContributions);
        Assert.Equal(TaxpayerCategory.DipendentePubblico, pub.Category);
    }

    [Fact]
    public void Cuneo_2025_gives_allowance_and_higher_net_than_2024()
    {
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 18_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxRate = 0m,
            MunicipalSurtaxRate = 0m,
        };

        var r2024 = _calc.Calculate(input, SampleRulesets.Year2024());
        var r2025 = _calc.Calculate(input, SampleRulesets.Year2025());

        // Nel 2025 scatta la "somma integrativa"; nel 2024 (reddito > 15.000) nessun bonus.
        Assert.Equal(0m, r2024.TreatmentBonus);
        Assert.True(r2025.TreatmentBonus > 0m);
        Assert.True(r2025.NetAnnualIncome > r2024.NetAnnualIncome);
    }

    [Fact]
    public void Deductions_never_make_irpef_negative()
    {
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 12_000m,
            EmploymentDays = 365,
            OtherTaxCredits = 100_000m, // detrazioni assurde
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2025());
        Assert.True(r.NetTax >= 0m);
    }

    [Fact]
    public void Complementary_pension_is_deducted_within_the_annual_cap()
    {
        var input = new EmploymentIncomeInput { GrossAnnualSalary = 40_000m, Sector = EmploymentSector.Privato, EmploymentDays = 365 };
        var baseline = _calc.Calculate(input, SampleRulesets.Year2025());

        // 3.000 € versati → imponibile ridotto di 3.000 → meno imposte.
        var withPension = _calc.Calculate(input with { ComplementaryPensionContribution = 3_000m }, SampleRulesets.Year2025());
        Assert.Equal(baseline.TaxableIncome - 3_000m, withPension.TaxableIncome);
        Assert.True(withPension.TotalTaxes < baseline.TotalTaxes);

        // Oltre il tetto (10.000 €) → dedotto solo il massimo (5.164,57 €).
        var overCap = _calc.Calculate(input with { ComplementaryPensionContribution = 10_000m }, SampleRulesets.Year2025());
        Assert.Equal(baseline.TaxableIncome - 5164.57m, overCap.TaxableIncome);
    }
}
