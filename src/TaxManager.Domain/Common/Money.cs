namespace TaxManager.Domain.Common;

/// <summary>
/// Helper di arrotondamento monetario.
/// Regola: si usa <c>decimal</c> ovunque (mai double/float) per il denaro.
/// Il fisco italiano arrotonda le imposte all'unità di euro; gli importi
/// intermedi (contributi, redditi) li teniamo a 2 decimali.
/// </summary>
public static class Money
{
    /// <summary>Arrotonda a 2 decimali (centesimi), modalità commerciale.</summary>
    public static decimal Cents(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>Arrotonda all'unità di euro (usato per le imposte).</summary>
    public static decimal Euro(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
}
