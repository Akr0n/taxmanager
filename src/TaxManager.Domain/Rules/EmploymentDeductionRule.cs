namespace TaxManager.Domain.Rules;

/// <summary>
/// Banda della detrazione da lavoro dipendente. La detrazione "a formula" diventa DATI:
/// importo base + parte che decresce linearmente nella banda.
/// <para>amount = Base + Taper · (TaperTo − reddito) / (TaperTo − TaperFrom)</para>
/// </summary>
public sealed record EmploymentDeductionBand(
    decimal IncomeUpTo,   // si applica quando reddito &lt;= IncomeUpTo
    decimal Base,         // parte fissa
    decimal Taper,        // parte variabile che si azzera salendo nella banda
    decimal TaperFrom,    // estremo inferiore del rapporto di decrescita
    decimal TaperTo);     // estremo superiore del rapporto di decrescita

/// <summary>
/// Detrazione per redditi da lavoro dipendente (insieme di bande + importo minimo),
/// rapportata ai giorni di lavoro nell'anno.
/// </summary>
public sealed record EmploymentDeductionRule(
    IReadOnlyList<EmploymentDeductionBand> Bands,
    decimal MinAmount)
{
    /// <param name="income">Reddito complessivo di riferimento.</param>
    /// <param name="employmentDays">Giorni di lavoro nell'anno (la detrazione è ragguagliata).</param>
    public decimal Compute(decimal income, int employmentDays)
    {
        if (income <= 0) return 0m;
        var band = Bands.FirstOrDefault(b => income <= b.IncomeUpTo);
        if (band is null) return 0m; // oltre l'ultima banda: nessuna detrazione

        var span = band.TaperTo - band.TaperFrom;
        var variable = span > 0
            ? band.Taper * (band.TaperTo - income) / span
            : band.Taper;

        var amount = Math.Max(band.Base + variable, 0m);
        if (amount > 0 && MinAmount > 0) amount = Math.Max(amount, MinAmount);

        var days = Math.Clamp(employmentDays, 0, 365);
        return amount * days / 365m;
    }
}
