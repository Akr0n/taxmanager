namespace TaxManager.Domain.Rules;

/// <summary>
/// Contributi previdenziali a carico del LAVORATORE DIPENDENTE (quota INPS trattenuta in busta paga).
/// <para>contributi = reddito·Rate (fino a <see cref="AnnualCeiling"/>) + parte oltre <see cref="AdditionalThreshold"/> · <see cref="AdditionalRate"/></para>
/// Per il privato la quota tipica è ~9,19% con +1% sulla parte eccedente la prima fascia.
/// </summary>
public sealed record EmployeeContributionRule(
    decimal Rate,
    decimal AdditionalRate,
    decimal AdditionalThreshold,
    decimal AnnualCeiling)
{
    public decimal Compute(decimal grossSalary)
    {
        if (grossSalary <= 0) return 0m;
        var capped = AnnualCeiling > 0 ? Math.Min(grossSalary, AnnualCeiling) : grossSalary;
        var main = capped * Rate;
        var additional = AdditionalThreshold > 0 && capped > AdditionalThreshold
            ? (capped - AdditionalThreshold) * AdditionalRate
            : 0m;
        return main + additional;
    }
}
