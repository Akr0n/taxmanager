using TaxManager.Application.Models;

namespace TaxManager.Application.Abstractions;

/// <summary>Persistenza dei profili contribuente su disco (file JSON).</summary>
public interface IProfileStore
{
    Task<IReadOnlyList<SavedProfile>> LoadAllAsync(CancellationToken ct = default);

    /// <summary>Inserisce o aggiorna il profilo (per <see cref="SavedProfile.Id"/>).</summary>
    Task SaveAsync(SavedProfile profile, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Percorso del file su disco che contiene i profili (mostrabile all'utente).</summary>
    string StorePath { get; }
}
