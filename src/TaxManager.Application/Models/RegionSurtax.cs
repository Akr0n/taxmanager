using TaxManager.Domain.Rules;

namespace TaxManager.Application.Models;

/// <summary>Addizionale regionale di una regione: tariffa a scaglioni (o flat) risolta per anno.</summary>
public sealed class RegionSurtax
{
    public required string Region { get; init; }

    /// <summary>Tariffa a scaglioni; una regione "flat" ha un solo scaglione.</summary>
    public required ProgressiveSchedule Schedule { get; init; }

    /// <summary>Aliquota del primo scaglione (per visualizzazione/persistenza sintetica).</summary>
    public decimal RepresentativeRate => Schedule.Brackets.Count > 0 ? Schedule.Brackets[0].Rate : 0m;

    /// <summary>True se l'aliquota varia per scaglioni di reddito.</summary>
    public bool IsProgressive => Schedule.Brackets.Count > 1;

    public override string ToString() => Region;
}
