using Microsoft.Extensions.DependencyInjection;
using TaxManager.Application.Services;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;

namespace TaxManager.Application;

/// <summary>Registrazione dei servizi del layer applicativo.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddTaxManagerApplication(this IServiceCollection services)
    {
        services.AddSingleton<ITaxCalculator<EmploymentIncomeInput>, EmploymentTaxCalculator>();
        services.AddSingleton<ITaxCalculator<ForfettarioIncomeInput>, ForfettarioTaxCalculator>();
        services.AddSingleton<ITaxCalculationService, TaxCalculationService>();
        return services;
    }
}
