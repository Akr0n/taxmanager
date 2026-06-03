using Microsoft.EntityFrameworkCore;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;

namespace TaxManager.Infrastructure.Persistence;

/// <summary>
/// Repository dello storico calcoli su SQLite. Usa una <see cref="IDbContextFactory{TContext}"/>
/// per creare un contesto per operazione (sicuro in un'app desktop multi-thread).
/// </summary>
public sealed class SqliteTaxComputationRepository : ITaxComputationRepository
{
    private readonly IDbContextFactory<TaxManagerDbContext> _factory;

    public SqliteTaxComputationRepository(IDbContextFactory<TaxManagerDbContext> factory) => _factory = factory;

    public async Task AddAsync(ComputationRecord record, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        db.Computations.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ComputationRecord>> GetRecentAsync(int take = 100, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Computations
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ThenByDescending(x => x.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<ComputationRecord?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.Computations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Computations.Where(x => x.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Computations.ExecuteDeleteAsync(ct);
    }
}
