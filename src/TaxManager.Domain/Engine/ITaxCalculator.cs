using TaxManager.Domain.Rules;
using TaxManager.Domain.Results;

namespace TaxManager.Domain.Engine;

/// <summary>Contratto generico di un calcolatore d'imposta: input tipizzato + ruleset dell'anno → esito.</summary>
public interface ITaxCalculator<in TInput>
{
    TaxResult Calculate(TInput input, TaxYearRuleset ruleset);
}
