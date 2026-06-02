// TaxManager.Domain — scheletro del modello di dominio (caso DIPENDENTE).
//
// Principi di design:
//   1) decimal ovunque per il denaro (MAI double/float).
//   2) Le regole sono DATI versionati per anno fiscale (TaxYearRuleset),
//      caricabili da JSON. I metodi di calcolo stanno sul TIPO, quindi un
//      record deserializzato da JSON conserva comunque i suoi metodi.
//   3) Tre concern separati: INPUT contribuente | REGOLE dell'anno | ENGINE.
//   4) L'output è una scomposizione passo-passo (auditabile), non un numero solo.
//
// ATTENZIONE: i valori numerici qui sotto sono ESEMPI ILLUSTRATIVI.
// Vanno verificati e aggiornati dalle norme ufficiali per ciascun anno.

namespace TaxManager.Domain;

// ---------------------------------------------------------------------------
// 1. PRIMITIVE DELLE REGOLE (dati puri, serializzabili)
// ---------------------------------------------------------------------------

/// <summary>Uno scaglione progressivo: aliquota marginale fino a un tetto.</summary>
public sealed record ProgressiveBracket(decimal UpTo, decimal Rate);
// L'ultimo scaglione usa UpTo = decimal.MaxValue.

/// <summary>Tariffa progressiva (IRPEF o addizionali). Pura funzione sui propri dati.</summary>
public sealed record ProgressiveSchedule(IReadOnlyList<ProgressiveBracket> Brackets)
{
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
}

/// <summary>
/// Banda della detrazione da lavoro dipendente. La detrazione "a formula"
/// diventa DATI: importo base + parte che decresce linearmente nella banda.
/// amount = Base + Taper * (TaperTo - reddito) / (TaperTo - TaperFrom)
/// </summary>
public sealed record EmploymentDeductionBand(
    decimal IncomeUpTo,   // si applica quando reddito <= IncomeUpTo
    decimal Base,         // parte fissa
    decimal Taper,        // parte variabile che si azzera salendo
    decimal TaperFrom,    // estremo inferiore del rapporto di decrescita
    decimal TaperTo);     // estremo superiore del rapporto di decrescita

/// <summary>Regola della detrazione da lavoro dipendente (insieme di bande + minimo).</summary>
public sealed record EmploymentDeductionRule(
    IReadOnlyList<EmploymentDeductionBand> Bands,
    decimal MinAmount)
{
    /// <param name="employmentDays">La detrazione è rapportata ai giorni di lavoro.</param>
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
        if (amount > 0) amount = Math.Max(amount, MinAmount);

        // ragguaglio ai giorni nell'anno
        return amount * employmentDays / 365m;
    }
}

/// <summary>Trattamento integrativo (condizionato a soglie di reddito/imposta).</summary>
public sealed record TreatmentBonusRule(decimal IncomeThreshold, decimal AnnualAmount)
{
    public decimal Compute(decimal income, int employmentDays) =>
        income <= IncomeThreshold ? AnnualAmount * employmentDays / 365m : 0m;
}

// ---------------------------------------------------------------------------
// 2. RULESET DELL'ANNO (la "fotografia" normativa di un anno fiscale)
// ---------------------------------------------------------------------------

public sealed record TaxYearRuleset
{
    public required int Year { get; init; }
    public required ProgressiveSchedule Irpef { get; init; }
    public required EmploymentDeductionRule EmploymentDeduction { get; init; }
    public TreatmentBonusRule? TreatmentBonus { get; init; }
}

/// <summary>
/// Le addizionali dipendono dalla residenza (regione/comune), quindi NON stanno
/// nel ruleset nazionale: si risolvono a parte e si passano al calcolo.
/// </summary>
public sealed record LocalSurtaxes(ProgressiveSchedule Regional, ProgressiveSchedule Municipal);

// ---------------------------------------------------------------------------
// 3. INPUT DEL CONTRIBUENTE (caso dipendente)
// ---------------------------------------------------------------------------

public sealed record EmploymentIncomeInput
{
    public required decimal GrossEmploymentIncome { get; init; } // reddito da lavoro dipendente
    public decimal DeductibleCharges { get; init; }              // oneri deducibili (abbattono l'imponibile)
    public decimal OtherTaxCredits { get; init; }                // altre detrazioni d'imposta (oneri 19% ecc.) — semplificato
    public int EmploymentDays { get; init; } = 365;              // giorni di lavoro nell'anno
    public required LocalSurtaxes Surtaxes { get; init; }
}

// ---------------------------------------------------------------------------
// 4. OUTPUT — scomposizione auditabile
// ---------------------------------------------------------------------------

public sealed record TaxResult
{
    public required decimal TaxableIncome { get; init; }      // imponibile
    public required decimal GrossTax { get; init; }           // imposta lorda
    public required decimal EmploymentDeduction { get; init; }
    public required decimal OtherTaxCredits { get; init; }
    public required decimal NetIrpef { get; init; }           // imposta netta (>= 0)
    public required decimal RegionalSurtax { get; init; }
    public required decimal MunicipalSurtax { get; init; }
    public required decimal TreatmentBonus { get; init; }
    public required decimal TotalDue { get; init; }
}

// ---------------------------------------------------------------------------
// 5. ENGINE — applica le regole. L'interfaccia generica è la "cucitura"
//    dove più avanti si innestano i calcolatori per Forfettario / Ordinario.
// ---------------------------------------------------------------------------

public interface ITaxCalculator<in TInput>
{
    TaxResult Calculate(TInput input, TaxYearRuleset ruleset);
}

public sealed class EmploymentTaxCalculator : ITaxCalculator<EmploymentIncomeInput>
{
    public TaxResult Calculate(EmploymentIncomeInput input, TaxYearRuleset ruleset)
    {
        var taxable = Math.Max(input.GrossEmploymentIncome - input.DeductibleCharges, 0m);

        var grossTax = ruleset.Irpef.ComputeTax(taxable);

        var workDeduction = ruleset.EmploymentDeduction.Compute(
            input.GrossEmploymentIncome, input.EmploymentDays);

        // imposta netta: lorda - detrazioni, mai sotto zero (le detrazioni non rimborsano)
        var netIrpef = Math.Max(grossTax - workDeduction - input.OtherTaxCredits, 0m);

        var regional = input.Surtaxes.Regional.ComputeTax(taxable);
        var municipal = input.Surtaxes.Municipal.ComputeTax(taxable);

        var bonus = ruleset.TreatmentBonus?.Compute(
            input.GrossEmploymentIncome, input.EmploymentDays) ?? 0m;

        var total = netIrpef + regional + municipal - bonus;

        // NB: l'arrotondamento (all'unità di euro per il saldo) è imposto dalle
        // norme e va applicato nei punti corretti — qui omesso per chiarezza.
        return new TaxResult
        {
            TaxableIncome = taxable,
            GrossTax = grossTax,
            EmploymentDeduction = workDeduction,
            OtherTaxCredits = input.OtherTaxCredits,
            NetIrpef = netIrpef,
            RegionalSurtax = regional,
            MunicipalSurtax = municipal,
            TreatmentBonus = bonus,
            TotalDue = total,
        };
    }
}

// ---------------------------------------------------------------------------
// 6. ESEMPIO DI RULESET (valori ILLUSTRATIVI, da sostituire con le norme reali)
// ---------------------------------------------------------------------------

public static class SampleRulesets
{
    public static TaxYearRuleset ExampleYear() => new()
    {
        Year = 2025,
        // Struttura a 3 scaglioni (forma attuale) — verificare aliquote/soglie per anno.
        Irpef = new ProgressiveSchedule(new ProgressiveBracket[]
        {
            new(UpTo: 28_000m,          Rate: 0.23m),
            new(UpTo: 50_000m,          Rate: 0.35m),
            new(UpTo: decimal.MaxValue, Rate: 0.43m),
        }),
        EmploymentDeduction = new EmploymentDeductionRule(
            Bands: new EmploymentDeductionBand[]
            {
                // esempi: una banda fissa, una con decrescita lineare, poi zero
                new(IncomeUpTo: 15_000m, Base: 1_955m, Taper: 0m,     TaperFrom: 0m,      TaperTo: 0m),
                new(IncomeUpTo: 28_000m, Base: 1_910m, Taper: 1_190m, TaperFrom: 15_000m, TaperTo: 28_000m),
                new(IncomeUpTo: 50_000m, Base: 0m,     Taper: 1_910m, TaperFrom: 28_000m, TaperTo: 50_000m),
            },
            MinAmount: 690m),
        TreatmentBonus = new TreatmentBonusRule(IncomeThreshold: 15_000m, AnnualAmount: 1_200m),
    };
}
