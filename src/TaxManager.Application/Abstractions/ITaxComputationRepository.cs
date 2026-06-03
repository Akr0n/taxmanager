using TaxManager.Application.Models;

namespace TaxManager.Application.Abstractions;

/// <summary>Persistenza dello storico dei calcoli su database relazionale.</summary>
public interface ITaxComputationRepository
{
    Task AddAsync(ComputationRecord record, CancellationToken ct = default);

    /// <summary>Ultimi calcoli, dal più recente.</summary>
    Task<IReadOnlyList<ComputationRecord>> GetRecentAsync(int take = 100, CancellationToken ct = default);

    Task<ComputationRecord?> GetByIdAsync(int id, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);
}
