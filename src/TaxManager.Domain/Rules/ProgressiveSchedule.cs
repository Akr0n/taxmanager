namespace TaxManager.Domain.Rules;

/// <summary>Uno scaglione progressivo: aliquota marginale fino a un tetto.</summary>
/// <remarks>L'ultimo scaglione usa <c>UpTo = decimal.MaxValue</c> (o un valore molto grande).</remarks>
public sealed record ProgressiveBracket(decimal UpTo, decimal Rate);

/// <summary>
/// Tariffa progressiva (IRPEF o addizionali a scaglioni). È una funzione pura sui propri dati,
/// quindi un'istanza deserializzata da JSON conserva comunque la capacità di calcolo.
/// </summary>
public sealed record ProgressiveSchedule(IReadOnlyList<ProgressiveBracket> Brackets)
{
    /// <summary>Imposta lorda sull'imponibile, applicando le aliquote marginali per scaglione.</summary>
    public decimal ComputeTax(decimal taxableIncome)
    {
        if (taxableIncome <= 0) return 0m;
        decimal tax = 0m, lower = 0m;
        foreach (var b in Brackets) // attesi ordinati per UpTo crescente
        {
            if (taxableIncome <= lower) break;
            var upper = Math.Min(taxableIncome, b.UpTo);
            tax += (upper - lower) * b.Rate;
            lower = b.UpTo;
        }
        return tax;
    }

    /// <summary>Aliquota marginale applicabile al livello di reddito indicato.</summary>
    public decimal MarginalRate(decimal taxableIncome)
    {
        decimal lower = 0m;
        foreach (var b in Brackets)
        {
            if (taxableIncome <= b.UpTo) return b.Rate;
            lower = b.UpTo;
        }
        return Brackets.Count > 0 ? Brackets[^1].Rate : 0m;
    }
}
