using TaxManager.Domain.Results;

namespace TaxManager.Application.Abstractions;

/// <summary>Esporta su disco il prospetto di un calcolo in formato leggibile.</summary>
public interface IReportExporter
{
    /// <summary>Scrive il prospetto come testo formattato nel percorso indicato.</summary>
    Task ExportTextAsync(TaxResult result, string filePath, CancellationToken ct = default);

    /// <summary>Scrive il prospetto come JSON nel percorso indicato.</summary>
    Task ExportJsonAsync(TaxResult result, string filePath, CancellationToken ct = default);
}
