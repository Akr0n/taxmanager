using System;
using System.IO;
using System.Linq;
using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;
using TaxManager.Infrastructure.Surtaxes;

namespace TaxManager.Domain.Tests;

public class SurtaxTests
{
    private static SurtaxJsonProvider Provider() =>
        new(Path.Combine(AppContext.BaseDirectory, "addizionali"));

    [Fact]
    public void Loads_region_province_municipality_levels()
    {
        var p = Provider();

        var regions = p.GetRegions(2025);
        Assert.NotEmpty(regions);
        Assert.Contains(regions, r => r.Region == "Lazio" && r.IsProgressive);

        Assert.Contains("Roma", p.GetProvinces(2025, "Lazio"));
        Assert.Contains(p.GetMunicipalities(2025, "Lazio", "Roma"), m => m.Name == "Roma");

        // Copertura ampia: la provincia di Torino conta centinaia di comuni.
        Assert.True(p.GetMunicipalities(2025, "Piemonte", "Torino").Count > 100);
    }

    [Fact]
    public void Progressive_regional_surtax_is_applied_marginally()
    {
        var calc = new EmploymentTaxCalculator();
        var lazio = Provider().GetRegions(2025).First(r => r.Region == "Lazio");

        var input = new EmploymentIncomeInput
        {
            GrossAnnualSalary = 30_000m,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            RegionalSurtaxSchedule = lazio.Schedule,
        };

        // imponibile 27.243 → 15.000*1,73% + 12.243*3,33% = 667,19 → 667
        Assert.Equal(667m, calc.Calculate(input, SampleRulesets.Year2025()).RegionalSurtax);
    }

    [Fact]
    public void Municipal_exemption_threshold_is_respected()
    {
        var calc = new EmploymentTaxCalculator();
        var milano = Provider().GetMunicipalities(2025, "Lombardia", "Milano").First(m => m.Name == "Milano");

        EmploymentIncomeInput Build(decimal ral) => new()
        {
            GrossAnnualSalary = ral,
            Sector = EmploymentSector.Privato,
            EmploymentDays = 365,
            MunicipalSurtaxSchedule = milano.Schedule,
            MunicipalSurtaxRate = milano.RepresentativeRate,
            MunicipalExemptionThreshold = milano.ExemptionThreshold,
        };

        // Milano: esente sotto la soglia → con RAL 18.000 (imponibile sotto soglia) comunale 0
        Assert.Equal(0m, calc.Calculate(Build(18_000m), SampleRulesets.Year2025()).MunicipalSurtax);
        // Con RAL 40.000 (imponibile oltre soglia) l'addizionale è dovuta
        Assert.True(calc.Calculate(Build(40_000m), SampleRulesets.Year2025()).MunicipalSurtax > 0m);
    }
}
