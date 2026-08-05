using System.Text.Json;
using TaxManager.Application.Models;
using TaxManager.Domain.Results;

namespace TaxManager.Application.Services;

/// <summary>Costruisce una <see cref="ComputationRecord"/> a partire da un esito di calcolo.</summary>
public static class ComputationRecordFactory
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static ComputationRecord Create(TaxResult result, string? profileName, object inputSnapshot) => new()
    {
        CreatedUtc = DateTime.UtcNow,
        ProfileName = profileName,
        Category = result.Category,
        Year = result.Year,
        GrossIncome = result.GrossIncome,
        TaxableIncome = result.TaxableIncome,
        SocialContributions = result.SocialContributions,
        TotalTaxes = result.TotalTaxes,
        NetAnnualIncome = result.NetAnnualIncome,
        EffectiveTaxRate = result.EffectiveTaxRate,
        InputJson = JsonSerializer.Serialize(inputSnapshot, JsonOptions),
        ResultJson = JsonSerializer.Serialize(result, JsonOptions),
    };

    /// <summary>
    /// Deserializza il prospetto completo salvato in <see cref="ComputationRecord.ResultJson"/>.
    /// Restituisce <c>null</c> anche se il JSON è corrotto, così una riga storico danneggiata
    /// non fa crashare la visualizzazione del dettaglio (degradazione sicura).
    /// </summary>
    public static TaxResult? ReadResult(ComputationRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.ResultJson)) return null;
        try { return JsonSerializer.Deserialize<TaxResult>(record.ResultJson, JsonOptions); }
        catch (JsonException) { return null; }
    }
}
