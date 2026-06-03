namespace TaxManager.Domain.Rules;

/// <summary>
/// Trattamento integrativo / "bonus" da lavoro dipendente, erogato quando il reddito
/// è entro una soglia. Importo annuo ragguagliato ai giorni di lavoro.
/// </summary>
public sealed record TreatmentBonusRule(decimal IncomeThreshold, decimal AnnualAmount)
{
    public decimal Compute(decimal income, int employmentDays)
    {
        if (income <= 0 || income > IncomeThreshold) return 0m;
        var days = Math.Clamp(employmentDays, 0, 365);
        return AnnualAmount * days / 365m;
    }
}
