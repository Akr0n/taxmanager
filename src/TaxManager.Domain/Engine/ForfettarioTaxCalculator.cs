using TaxManager.Domain.Common;
using TaxManager.Domain.Inputs;
using TaxManager.Domain.Results;
using TaxManager.Domain.Rules;

namespace TaxManager.Domain.Engine;

/// <summary>
/// Calcolo per il regime forfettario.
/// Flusso: ricavi → reddito forfettario (ricavi·coefficiente) → contributi previdenziali →
/// base imponibile (reddito − contributi deducibili) → imposta sostitutiva (15% o 5% start-up).
/// Nessuna IRPEF, addizionale o IVA.
/// </summary>
public sealed class ForfettarioTaxCalculator : ITaxCalculator<ForfettarioIncomeInput>
{
    public TaxResult Calculate(ForfettarioIncomeInput input, TaxYearRuleset ruleset)
    {
        var f = ruleset.Forfettario;
        var revenue = Math.Max(input.Revenue, 0m);
        var coeff = Math.Clamp(input.Coefficient, 0m, 1m);

        // Reddito forfettario = base di calcolo dei contributi (al lordo dei contributi stessi).
        var redditoLordo = Money.Cents(revenue * coeff);

        var contributi = input.Scheme switch
        {
            ForfettarioContributionScheme.GestioneSeparata => redditoLordo * f.GestioneSeparataRate,
            ForfettarioContributionScheme.Artigiani => f.Artigiani.Compute(redditoLordo, input.ApplyArtigianiCommerciantiReduction),
            ForfettarioContributionScheme.Commercianti => f.Commercianti.Compute(redditoLordo, input.ApplyArtigianiCommerciantiReduction),
            ForfettarioContributionScheme.CassaProfessionale => redditoLordo * Math.Max(input.CassaRate, 0m),
            _ => 0m
        };
        contributi = Money.Cents(contributi);

        // I contributi versati nell'anno sono deducibili dal reddito.
        var contributiDeducibili = input.PaidContributionsDeductibleOverride is { } ov
            ? Math.Max(ov, 0m)
            : contributi;

        var baseImponibile = Math.Max(redditoLordo - contributiDeducibili, 0m);
        var rate = input.IsStartup ? f.StartupReducedRate : f.SubstituteTaxRate;
        var imposta = Money.Euro(baseImponibile * rate);

        var totalTaxes = imposta;
        var netIncome = Money.Cents(revenue - contributi - imposta);
        var effective = revenue > 0 ? imposta / revenue : 0m;
        var burden = revenue > 0 ? (imposta + contributi) / revenue : 0m;

        var schemeLabel = input.Scheme switch
        {
            ForfettarioContributionScheme.GestioneSeparata => $"Gestione Separata INPS ({f.GestioneSeparataRate:P2})",
            ForfettarioContributionScheme.Artigiani => "Gestione artigiani INPS",
            ForfettarioContributionScheme.Commercianti => "Gestione commercianti INPS",
            ForfettarioContributionScheme.CassaProfessionale => $"Cassa professionale ({input.CassaRate:P2})",
            _ => "Contributi previdenziali"
        };

        var lines = new List<TaxLineItem>
        {
            new("RIC", "Ricavi / compensi", revenue, TaxLineKind.Reddito),
            new("RED_F", $"Reddito forfettario (coeff. {coeff:P0})", redditoLordo, TaxLineKind.BaseImponibile),
            new("CONTR", schemeLabel, contributi, TaxLineKind.Contributo),
            new("BASE_IMP", "Base imponibile (reddito − contributi)", baseImponibile, TaxLineKind.BaseImponibile),
            new("IMP_SOST", $"Imposta sostitutiva ({rate:P0})", imposta, TaxLineKind.Imposta),
            new("TOT_IMP", "Totale imposte", totalTaxes, TaxLineKind.Totale),
            new("NETTO", "Reddito netto annuo", netIncome, TaxLineKind.Netto),
        };

        var notes = new List<string>();
        if (revenue > f.RevenueLimit)
            notes.Add($"Ricavi oltre il limite del regime forfettario (€{f.RevenueLimit:N0}): verificare la permanenza nel regime.");
        if (input.IsStartup)
            notes.Add($"Aliquota ridotta start-up applicata ({f.StartupReducedRate:P0}), valida per i primi {f.StartupYears} anni se sussistono i requisiti.");
        if (input.PaidContributionsDeductibleOverride is null)
            notes.Add("Contributi dedotti = contributi stimati dell'anno; legalmente sono deducibili i contributi effettivamente VERSATI nell'anno.");

        return new TaxResult
        {
            Category = TaxpayerCategory.Forfettario,
            Year = ruleset.Year,
            GrossIncome = revenue,
            SocialContributions = contributi,
            TaxableIncome = baseImponibile,
            GrossTax = imposta,
            Deductions = 0m,
            NetTax = imposta,
            RegionalSurtax = 0m,
            MunicipalSurtax = 0m,
            TreatmentBonus = 0m,
            TotalTaxes = totalTaxes,
            NetAnnualIncome = netIncome,
            EffectiveTaxRate = effective,
            TotalBurdenRate = burden,
            Lines = lines,
            Notes = notes,
        };
    }
}
