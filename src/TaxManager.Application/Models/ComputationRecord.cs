using TaxManager.Domain;

namespace TaxManager.Application.Models;

/// <summary>
/// Riga dello storico dei calcoli, persistita nel DB relazionale. Conserva i totali sintetici
/// (per liste/ricerche) e gli snapshot JSON di input ed esito (per ri-visualizzare il dettaglio).
/// </summary>
public sealed class ComputationRecord
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string? ProfileName { get; set; }

    public TaxpayerCategory Category { get; set; }
    public int Year { get; set; }

    public decimal GrossIncome { get; set; }
    public decimal TaxableIncome { get; set; }
    public decimal SocialContributions { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal NetAnnualIncome { get; set; }
    public decimal EffectiveTaxRate { get; set; }

    /// <summary>Snapshot JSON dell'input usato (per ricostruire la simulazione).</summary>
    public string InputJson { get; set; } = "{}";

    /// <summary>Snapshot JSON del <c>TaxResult</c> completo (per il prospetto dettagliato).</summary>
    public string ResultJson { get; set; } = "{}";
}
