using TaxManager.Domain.Rules;

namespace TaxManager.Application.Models;

/// <summary>Addizionale comunale di un comune: tariffa a scaglioni (o flat) + soglia di esenzione.</summary>
public sealed class MunicipalitySurtax
{
    public required string Region { get; init; }
    public required string Province { get; init; }
    public required string Name { get; init; }

    /// <summary>Tariffa a scaglioni; un comune ad aliquota unica ha un solo scaglione.</summary>
    public required ProgressiveSchedule Schedule { get; init; }

    /// <summary>Soglia di esenzione: sotto questo imponibile l'addizionale non è dovuta (0 = nessuna).</summary>
    public decimal ExemptionThreshold { get; init; }

    /// <summary>True per la voce "Altro comune": l'utente inserisce manualmente l'aliquota.</summary>
    public bool IsManualEntry { get; init; }

    /// <summary>Aliquota dell'ultimo scaglione (per visualizzazione).</summary>
    public decimal RepresentativeRate => Schedule.Brackets.Count > 0 ? Schedule.Brackets[^1].Rate : 0m;

    /// <summary>True se l'aliquota varia per scaglioni.</summary>
    public bool IsProgressive => Schedule.Brackets.Count > 1;

    public override string ToString() => Name;

    /// <summary>Voce sentinella per l'inserimento manuale dell'aliquota.</summary>
    public static MunicipalitySurtax Manual { get; } = new()
    {
        Region = string.Empty,
        Province = string.Empty,
        Name = "Altro comune (inserisci aliquota)…",
        Schedule = new ProgressiveSchedule(System.Array.Empty<ProgressiveBracket>()),
        IsManualEntry = true,
    };
}
