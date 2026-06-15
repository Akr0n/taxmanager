using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;

namespace TaxManager.Domain.Tests;

/// <summary>
/// Verifica le condizioni di CAPIENZA per dipendenti: in incapienza IRPEF (imposta netta = 0)
/// non sono dovute le addizionali regionale/comunale né spetta il trattamento integrativo (primo binario).
/// </summary>
public class IncapienzaAndReliefsTests
{
    private readonly EmploymentTaxCalculator _calc = new();

    [Fact]
    public void Addizionali_non_dovute_quando_irpef_netta_e_zero()
    {
        // RAL 9.000 (2024): imponibile 8.172,90; IRPEF lorda 1.879,77 < detrazione lavoro 1.955
        // → IRPEF netta = 0 (incapienza). In incapienza le addizionali NON sono dovute.
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 9_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxRate = 0.0123m,
            MunicipalSurtaxRate = 0.0060m,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.Equal(0m, r.NetTax);          // incapiente
        Assert.Equal(0m, r.RegionalSurtax);  // niente addizionale regionale in incapienza
        Assert.Equal(0m, r.MunicipalSurtax); // niente addizionale comunale in incapienza
    }

    [Fact]
    public void Addizionali_dovute_quando_irpef_netta_positiva()
    {
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 30_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxRate = 0.0123m,
            MunicipalSurtaxRate = 0.0060m,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.True(r.NetTax > 0m);
        Assert.True(r.RegionalSurtax > 0m);
        Assert.True(r.MunicipalSurtax > 0m);
    }

    [Fact]
    public void Trattamento_integrativo_non_spetta_in_incapienza()
    {
        // RAL 8.000 (2024): imponibile 7.264,80; IRPEF lorda 1.670,90 < detrazione lavoro 1.955
        // → incapiente: il trattamento integrativo (primo binario, reddito ≤ 15.000) NON spetta.
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 8_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.Equal(0m, r.TreatmentBonus);
    }

    [Fact]
    public void Trattamento_integrativo_spetta_quando_capiente()
    {
        // RAL 14.000 (2024): imponibile 12.713,40; IRPEF lorda 2.924,08 > detrazione lavoro 1.955
        // → capiente: il trattamento integrativo (1.200) spetta.
        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 14_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
        };

        var r = _calc.Calculate(input, SampleRulesets.Year2024());

        Assert.Equal(1_200m, r.TreatmentBonus);
    }
}
