using TaxManager.Application.Models;
using TaxManager.Application.Services;
using TaxManager.Domain;

namespace TaxManager.Domain.Tests;

public class ApplicationServiceTests
{
    [Fact]
    public void ProfileMapper_roundtrips_employee_fields()
    {
        var profile = new SavedProfile
        {
            Category = TaxpayerCategory.DipendentePubblico,
            GrossAnnualSalary = 33_000m,
            EmploymentDays = 300,
            RegionalSurtaxRate = 0.0173m,
            MunicipalSurtaxRate = 0.008m,
            DeductibleCharges = 500m,
            OtherTaxCredits = 200m,
        };

        var input = ProfileMapper.ToEmployment(profile);

        Assert.Equal(EmploymentSector.Pubblico, input.Sector);
        Assert.Equal(33_000m, input.GrossAnnualSalary);
        Assert.Equal(300, input.EmploymentDays);
        Assert.Equal(0.0173m, input.RegionalSurtaxRate);
        Assert.Equal(500m, input.DeductibleCharges);
    }

    [Fact]
    public void SampleRulesets_exposes_2024_and_2025()
    {
        Assert.Contains(2024, SampleRulesets.AvailableYears);
        Assert.Contains(2025, SampleRulesets.AvailableYears);
    }
}
