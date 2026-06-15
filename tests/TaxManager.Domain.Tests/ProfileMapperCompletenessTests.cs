using TaxManager.Application.Models;
using TaxManager.Application.Services;
using TaxManager.Domain;

namespace TaxManager.Domain.Tests;

/// <summary>
/// Il mapping profilo→input del dominio deve riportare TUTTI i campi rilevanti, inclusi la soglia
/// di esenzione comunale e la previdenza complementare (altrimenti il ricalcolo da profilo diverge).
/// </summary>
public class ProfileMapperCompletenessTests
{
    [Fact]
    public void ToEmployment_maps_municipal_exemption_and_complementary_pension()
    {
        var profile = new SavedProfile
        {
            Category = TaxpayerCategory.DipendentePrivato,
            GrossAnnualSalary = 30_000m,
            MunicipalExemptionThreshold = 12_000m,
            PensionContribution = 2_000m,
        };

        var input = ProfileMapper.ToEmployment(profile);

        Assert.Equal(12_000m, input.MunicipalExemptionThreshold);
        Assert.Equal(2_000m, input.ComplementaryPensionContribution);
    }
}
