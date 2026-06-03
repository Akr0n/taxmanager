using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;

namespace TaxManager.Domain.Tests;

public class ForfettarioTaxCalculatorTests
{
    private readonly ForfettarioTaxCalculator _calc = new();

    [Fact]
    public void Gestione_separata_2024_full_breakdown()
    {
        var input = new ForfettarioIncomeInput
        {
            Revenue = 50_000m,
            Coefficient = 0.78m,
            Scheme = ForfettarioContributionScheme.GestioneSeparata,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.Equal(TaxpayerCategory.Forfettario, r.Category);
        Assert.Equal(10167.30m, r.SocialContributions);  // 39.000 * 26,07%
        Assert.Equal(28832.70m, r.TaxableIncome);         // reddito - contributi
        Assert.Equal(4325m, r.NetTax);                    // 28.832,70 * 15% arrotondato
        Assert.Equal(35507.70m, r.NetAnnualIncome);
        Assert.Equal(0m, r.RegionalSurtax);               // niente addizionali nel forfettario
    }

    [Fact]
    public void Startup_uses_reduced_5_percent_rate()
    {
        var input = new ForfettarioIncomeInput
        {
            Revenue = 50_000m,
            Coefficient = 0.78m,
            Scheme = ForfettarioContributionScheme.GestioneSeparata,
            IsStartup = true,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        // 28.832,70 * 5% = 1.441,635 -> 1.442
        Assert.Equal(1442m, r.NetTax);
    }

    [Fact]
    public void Artigiani_reduction_35_percent_lowers_contributions()
    {
        var baseInput = new ForfettarioIncomeInput
        {
            Revenue = 50_000m,
            Coefficient = 0.40m, // reddito forfettario 20.000
            Scheme = ForfettarioContributionScheme.Artigiani,
        };

        var full = _calc.Calculate(baseInput, SampleRulesets.Year2024());
        var reduced = _calc.Calculate(baseInput with { ApplyArtigianiCommerciantiReduction = true }, SampleRulesets.Year2024());

        Assert.Equal(4823.29m, full.SocialContributions);   // 4.427,04 + (20.000-18.415)*25%
        Assert.Equal(3135.14m, reduced.SocialContributions); // * 0,65
    }

    [Fact]
    public void Revenue_over_limit_adds_warning_note()
    {
        var input = new ForfettarioIncomeInput
        {
            Revenue = 90_000m, // oltre 85.000
            Coefficient = 0.78m,
            Scheme = ForfettarioContributionScheme.GestioneSeparata,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2025());
        Assert.Contains(r.Notes, n => n.Contains("limite", System.StringComparison.OrdinalIgnoreCase));
    }
}
