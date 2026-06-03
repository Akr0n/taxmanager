using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;
using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;
using TaxManager.Domain.Results;
using TaxManager.Domain.Rules;

namespace TaxManager.Application.Services;

/// <inheritdoc />
public sealed class TaxCalculationService : ITaxCalculationService
{
    private readonly IRulesetProvider _rulesets;
    private readonly ITaxCalculator<EmploymentIncomeInput> _employment;
    private readonly ITaxCalculator<ForfettarioIncomeInput> _forfettario;

    public TaxCalculationService(
        IRulesetProvider rulesets,
        ITaxCalculator<EmploymentIncomeInput> employment,
        ITaxCalculator<ForfettarioIncomeInput> forfettario)
    {
        _rulesets = rulesets;
        _employment = employment;
        _forfettario = forfettario;
    }

    public IReadOnlyList<int> AvailableYears => _rulesets.AvailableYears;

    public TaxYearRuleset GetRuleset(int year) => _rulesets.Get(year);

    public TaxResult Calculate(EmploymentIncomeInput input, int year) =>
        _employment.Calculate(input, _rulesets.Get(year));

    public TaxResult Calculate(ForfettarioIncomeInput input, int year) =>
        _forfettario.Calculate(input, _rulesets.Get(year));

    public TaxResult CalculateFromProfile(SavedProfile profile) => profile.Category switch
    {
        TaxpayerCategory.Forfettario => Calculate(ProfileMapper.ToForfettario(profile), profile.Year),
        _ => Calculate(ProfileMapper.ToEmployment(profile), profile.Year),
    };
}
