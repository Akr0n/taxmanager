namespace TaxManager.Domain.Rules;

/// <summary>Fascia a percentuale sul reddito da lavoro (per la somma integrativa).</summary>
public sealed record EmploymentBonusBand(decimal IncomeUpTo, decimal Rate);

/// <summary>
/// "Somma integrativa" del cuneo fiscale 2025 (L. 207/2024): bonus NON imponibile erogato al
/// lavoratore se il reddito è entro <see cref="IncomeCeiling"/> (es. 20.000 €), pari a una
/// percentuale del reddito da lavoro variabile per fascia.
/// </summary>
public sealed record IntegrativeAllowanceRule(
    decimal IncomeCeiling,
    IReadOnlyList<EmploymentBonusBand> Bands)
{
    public decimal Compute(decimal employmentIncome, int employmentDays)
    {
        if (employmentIncome <= 0 || employmentIncome > IncomeCeiling) return 0m;
        var band = Bands.FirstOrDefault(b => employmentIncome <= b.IncomeUpTo);
        var rate = band?.Rate ?? 0m;
        var days = Math.Clamp(employmentDays, 0, 365);
        return employmentIncome * rate * days / 365m;
    }
}

/// <summary>
/// "Ulteriore detrazione" del cuneo fiscale 2025 per redditi medi: importo fisso
/// (<see cref="FlatAmount"/>, es. 1.000 €) per reddito tra <see cref="FromIncome"/> e
/// <see cref="FlatUpTo"/>, poi decrescente linearmente fino ad azzerarsi a <see cref="TaperTo"/>.
/// </summary>
public sealed record AdditionalDeductionRule(
    decimal FromIncome,
    decimal FlatUpTo,
    decimal FlatAmount,
    decimal TaperTo)
{
    public decimal Compute(decimal totalIncome)
    {
        if (totalIncome <= FromIncome || totalIncome > TaperTo) return 0m;
        if (totalIncome <= FlatUpTo) return FlatAmount;
        var span = TaperTo - FlatUpTo;
        return span > 0 ? FlatAmount * (TaperTo - totalIncome) / span : 0m;
    }
}
