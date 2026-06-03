using TaxManager.Domain;
using TaxManager.Domain.Rules;

namespace TaxManager.Domain.Tests;

public class ProgressiveScheduleTests
{
    private static ProgressiveSchedule Irpef() => SampleRulesets.Year2025().Irpef;

    [Theory]
    [InlineData(0, 0)]
    [InlineData(28000, 6440)]      // 28.000 * 23%
    [InlineData(30000, 7140)]      // 6.440 + 2.000 * 35%
    [InlineData(50000, 14140)]     // 6.440 + 22.000 * 35%
    [InlineData(60000, 18440)]     // 14.140 + 10.000 * 43%
    public void ComputeTax_matches_irpef_brackets(decimal income, decimal expected)
    {
        Assert.Equal(expected, Irpef().ComputeTax(income));
    }

    [Theory]
    [InlineData(10000, 0.23)]
    [InlineData(40000, 0.35)]
    [InlineData(90000, 0.43)]
    public void MarginalRate_returns_bracket_rate(decimal income, decimal expected)
    {
        Assert.Equal(expected, Irpef().MarginalRate(income));
    }

    [Fact]
    public void Negative_income_yields_zero_tax()
    {
        Assert.Equal(0m, Irpef().ComputeTax(-5000m));
    }

    [Fact]
    public void Irpef_2026_middle_bracket_lowered_to_33_percent()
    {
        // 30.000 € → 28.000·23% + 2.000·33% = 7.100 (2026) vs 7.140 (2025, 35%)
        Assert.Equal(7100m, SampleRulesets.Year2026().Irpef.ComputeTax(30_000m));
        Assert.Equal(7140m, SampleRulesets.Year2025().Irpef.ComputeTax(30_000m));
    }
}
