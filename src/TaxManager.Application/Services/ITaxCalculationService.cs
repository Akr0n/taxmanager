using TaxManager.Application.Models;
using TaxManager.Domain.Inputs;
using TaxManager.Domain.Results;
using TaxManager.Domain.Rules;

namespace TaxManager.Application.Services;

/// <summary>Caso d'uso centrale: risolve il ruleset dell'anno e applica il calcolatore giusto.</summary>
public interface ITaxCalculationService
{
    IReadOnlyList<int> AvailableYears { get; }

    TaxYearRuleset GetRuleset(int year);

    TaxResult Calculate(EmploymentIncomeInput input, int year);

    TaxResult Calculate(ForfettarioIncomeInput input, int year);

    /// <summary>Calcola a partire da un profilo salvato, scegliendo il calcolatore dalla categoria.</summary>
    TaxResult CalculateFromProfile(SavedProfile profile);
}
