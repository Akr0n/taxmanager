using System;
using System.IO;
using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;
using TaxManager.Infrastructure.Rulesets;

namespace TaxManager.Domain.Tests;

/// <summary>
/// Verifica che i file JSON in data/rulesets vengano caricati e mappati correttamente sul dominio,
/// e che NON divergano dal fallback in codice (<see cref="SampleRulesets"/>).
/// </summary>
public class RulesetLoadingTests
{
    private static JsonRulesetProvider Provider() =>
        new(Path.Combine(AppContext.BaseDirectory, "rulesets"));

    [Fact]
    public void Loads_both_years_with_expected_structure()
    {
        var provider = Provider();

        Assert.Contains(2024, provider.AvailableYears);
        Assert.Contains(2025, provider.AvailableYears);

        var r2025 = provider.Get(2025);
        Assert.NotNull(r2025.IntegrativeAllowance);  // cuneo 2025
        Assert.NotNull(r2025.AdditionalDeduction);
        Assert.Equal(9, r2025.Forfettario.Coefficients.Count);

        var r2024 = provider.Get(2024);
        Assert.Null(r2024.IntegrativeAllowance);      // assente nel 2024
    }

    [Fact]
    public void Json_rulesets_match_code_fallback_for_employee()
    {
        var calc = new EmploymentTaxCalculator();
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 35_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxRate = 0.0123m,
            MunicipalSurtaxRate = 0.0060m,
        };

        foreach (var year in new[] { 2024, 2025, 2026 })
        {
            var fromJson = calc.Calculate(input, Provider().Get(year));
            var fromCode = calc.Calculate(input, SampleRulesets.For(year));
            Assert.Equal(fromCode.TotalTaxes, fromJson.TotalTaxes);
            Assert.Equal(fromCode.NetAnnualIncome, fromJson.NetAnnualIncome);
        }
    }

    [Fact]
    public void Json_rulesets_match_code_fallback_for_forfettario()
    {
        var calc = new ForfettarioTaxCalculator();
        var input = new ForfettarioIncomeInput
        {
            Revenue = 60_000m,
            Coefficient = 0.78m,
            Scheme = ForfettarioContributionScheme.GestioneSeparata,
        };

        var fromJson = calc.Calculate(input, Provider().Get(2025));
        var fromCode = calc.Calculate(input, SampleRulesets.Year2025());
        Assert.Equal(fromCode.NetTax, fromJson.NetTax);
        Assert.Equal(fromCode.SocialContributions, fromJson.SocialContributions);
    }
}
