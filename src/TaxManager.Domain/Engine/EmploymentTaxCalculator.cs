using TaxManager.Domain.Common;
using TaxManager.Domain.Inputs;
using TaxManager.Domain.Results;
using TaxManager.Domain.Rules;

namespace TaxManager.Domain.Engine;

/// <summary>
/// Calcolo IRPEF + addizionali + contributi per il lavoratore dipendente (pubblico/privato).
/// Flusso: RAL → contributi → imponibile → IRPEF lorda → detrazioni (lavoro + ulteriore 2025) →
/// IRPEF netta → addizionali → trattamento integrativo + somma integrativa 2025 → netto in tasca.
/// </summary>
public sealed class EmploymentTaxCalculator : ITaxCalculator<EmploymentIncomeInput>
{
    public TaxResult Calculate(EmploymentIncomeInput input, TaxYearRuleset ruleset)
    {
        var ral = Math.Max(input.GrossAnnualSalary, 0m);

        var isPublic = input.Sector == EmploymentSector.Pubblico;
        var contribRule = isPublic ? ruleset.PublicEmployeeContribution : ruleset.PrivateEmployeeContribution;
        var contributi = Money.Cents(contribRule.Compute(ral));

        // Previdenza complementare: deducibile entro il tetto annuo (art. 8 D.Lgs. 252/2005).
        var pensionDeductible = Math.Min(Math.Max(input.ComplementaryPensionContribution, 0m), ruleset.ComplementaryPensionCap);

        // Imponibile IRPEF (≈ reddito complessivo) = RAL − contributi − oneri deducibili − previdenza complementare.
        var taxable = Math.Max(ral - contributi - input.DeductibleCharges - pensionDeductible, 0m);

        var grossTax = ruleset.Irpef.ComputeTax(taxable);
        var detrazioneLavoro = ruleset.EmploymentDeduction.Compute(taxable, input.EmploymentDays);
        var ulterioreDetrazione = ruleset.AdditionalDeduction?.Compute(taxable) ?? 0m;
        var otherCredits = Math.Max(input.OtherTaxCredits, 0m);

        var detrazioniTotali = detrazioneLavoro + ulterioreDetrazione + otherCredits;
        var netIrpef = Money.Euro(Math.Max(grossTax - detrazioniTotali, 0m));

        // Addizionale regionale: tariffa a scaglioni (risolta dalla regione) se presente, altrimenti aliquota flat.
        var regionalRaw = input.RegionalSurtaxSchedule is { } regSchedule
            ? regSchedule.ComputeTax(taxable)
            : taxable * Math.Max(input.RegionalSurtaxRate, 0m);
        var regional = Money.Euro(regionalRaw);

        // Addizionale comunale: nulla sotto la soglia di esenzione; altrimenti tariffa a scaglioni o aliquota flat.
        decimal municipalRaw;
        if (input.MunicipalExemptionThreshold > 0m && taxable <= input.MunicipalExemptionThreshold)
            municipalRaw = 0m;
        else if (input.MunicipalSurtaxSchedule is { } munSchedule)
            municipalRaw = munSchedule.ComputeTax(taxable);
        else
            municipalRaw = taxable * Math.Max(input.MunicipalSurtaxRate, 0m);
        var municipal = Money.Euro(municipalRaw);

        var trattamento = ruleset.TreatmentBonus?.Compute(taxable, input.EmploymentDays) ?? 0m;
        var sommaIntegrativa = ruleset.IntegrativeAllowance?.Compute(taxable, input.EmploymentDays) ?? 0m;
        var bonus = Money.Euro(trattamento + sommaIntegrativa);

        var totalTaxes = netIrpef + regional + municipal - bonus;
        var netIncome = Money.Cents(ral - contributi - totalTaxes);
        var effective = ral > 0 ? totalTaxes / ral : 0m;
        var burden = ral > 0 ? (totalTaxes + contributi) / ral : 0m;

        // Aliquote effettive per le etichette del prospetto, ricavate dagli importi (valide sia flat sia a scaglioni).
        var regionalEff = taxable > 0 ? regional / taxable : 0m;
        var municipalEff = taxable > 0 ? municipal / taxable : 0m;

        var category = isPublic ? TaxpayerCategory.DipendentePubblico : TaxpayerCategory.DipendentePrivato;

        var lines = new List<TaxLineItem>
        {
            new("RAL", "Reddito annuo lordo (RAL)", ral, TaxLineKind.Reddito),
            new("INPS", $"Contributi previdenziali ({contribRule.Rate:P2})", contributi, TaxLineKind.Contributo),
        };
        if (input.DeductibleCharges > 0)
            lines.Add(new("ONERI_DED", "Oneri deducibili", Money.Cents(input.DeductibleCharges), TaxLineKind.Detrazione));
        if (pensionDeductible > 0)
            lines.Add(new("PREV_COMP", "Previdenza complementare (dedotta)", Money.Cents(pensionDeductible), TaxLineKind.Detrazione));
        lines.Add(new("IMP", "Imponibile IRPEF", taxable, TaxLineKind.BaseImponibile));
        lines.Add(new("IRPEF_L", "IRPEF lorda", Money.Cents(grossTax), TaxLineKind.Imposta));
        lines.Add(new("DETR_LAV", "Detrazione lavoro dipendente", Money.Cents(detrazioneLavoro), TaxLineKind.Detrazione));
        if (ulterioreDetrazione > 0)
            lines.Add(new("DETR_ULT", "Ulteriore detrazione redditi medi", Money.Cents(ulterioreDetrazione), TaxLineKind.Detrazione));
        if (otherCredits > 0)
            lines.Add(new("DETR_ALT", "Altre detrazioni d'imposta", otherCredits, TaxLineKind.Detrazione));
        lines.Add(new("IRPEF_N", "IRPEF netta", netIrpef, TaxLineKind.Imposta));
        lines.Add(new("ADD_REG", $"Addizionale regionale ({regionalEff:P2})", regional, TaxLineKind.Addizionale));
        lines.Add(new("ADD_COM", $"Addizionale comunale ({municipalEff:P2})", municipal, TaxLineKind.Addizionale));
        if (trattamento > 0)
            lines.Add(new("TRATT", "Trattamento integrativo", Money.Euro(trattamento), TaxLineKind.Credito));
        if (sommaIntegrativa > 0)
            lines.Add(new("SOMMA", "Somma integrativa (cuneo fiscale)", Money.Euro(sommaIntegrativa), TaxLineKind.Credito));
        lines.Add(new("TOT_IMP", "Totale imposte (al netto dei bonus)", totalTaxes, TaxLineKind.Totale));
        lines.Add(new("NETTO", "Reddito netto annuo", netIncome, TaxLineKind.Netto));

        var notes = new List<string>();
        if (input.ComplementaryPensionContribution > ruleset.ComplementaryPensionCap)
            notes.Add($"Previdenza complementare: dedotto il massimo annuo (€{ruleset.ComplementaryPensionCap:N2}); l'eccedenza non è deducibile.");
        if (ruleset.TreatmentBonus is not null && trattamento == 0 && taxable > ruleset.TreatmentBonus.IncomeThreshold)
            notes.Add("Trattamento integrativo non spettante: reddito oltre la soglia prevista.");
        notes.Add("Stima annua semplificata: non considera conguagli, familiari a carico (assegno unico), esonero contributivo 2024 e altri crediti particolari.");

        return new TaxResult
        {
            Category = category,
            Year = ruleset.Year,
            GrossIncome = ral,
            SocialContributions = contributi,
            TaxableIncome = taxable,
            GrossTax = Money.Cents(grossTax),
            Deductions = Money.Cents(detrazioniTotali),
            NetTax = netIrpef,
            RegionalSurtax = regional,
            MunicipalSurtax = municipal,
            TreatmentBonus = bonus,
            TotalTaxes = totalTaxes,
            NetAnnualIncome = netIncome,
            EffectiveTaxRate = effective,
            TotalBurdenRate = burden,
            Lines = lines,
            Notes = notes,
        };
    }
}
