using TaxManager.Application.Models;
using TaxManager.Domain;
using TaxManager.Domain.Inputs;

namespace TaxManager.Application.Services;

/// <summary>Conversioni tra il profilo salvato (persistenza) e gli input del dominio.</summary>
public static class ProfileMapper
{
    public static EmploymentIncomeInput ToEmployment(SavedProfile p) => new()
    {
        GrossAnnualSalary = p.GrossAnnualSalary,
        Sector = p.Category == TaxpayerCategory.DipendentePubblico ? EmploymentSector.Pubblico : EmploymentSector.Privato,
        EmploymentDays = p.EmploymentDays <= 0 ? 365 : p.EmploymentDays,
        RegionalSurtaxRate = p.RegionalSurtaxRate,
        MunicipalSurtaxRate = p.MunicipalSurtaxRate,
        MunicipalExemptionThreshold = p.MunicipalExemptionThreshold,
        DeductibleCharges = p.DeductibleCharges,
        OtherTaxCredits = p.OtherTaxCredits,
        ComplementaryPensionContribution = p.PensionContribution,
    };

    public static ForfettarioIncomeInput ToForfettario(SavedProfile p) => new()
    {
        Revenue = p.Revenue,
        Coefficient = p.Coefficient,
        IsStartup = p.IsStartup,
        Scheme = p.Scheme,
        ApplyArtigianiCommerciantiReduction = p.ApplyContributionReduction,
        CassaRate = p.CassaRate,
    };
}
