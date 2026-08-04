using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaxManager.App.ViewModels;
using TaxManager.Application.Abstractions;
using TaxManager.Application.Models;
using TaxManager.Application.Services;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Results;
using TaxManager.Infrastructure.Rulesets;
using TaxManager.Infrastructure.Surtaxes;

namespace TaxManager.App.Tests;

/// <summary>Costruzione del <see cref="MainWindowViewModel"/> con provider reali (ruleset/addizionali
/// copiati nell'output) e doppi di test per la persistenza/esportazione.</summary>
internal static class VmFactory
{
    public static MainWindowViewModel Create(IReportExporter exporter, out InMemoryComputationRepository repo)
    {
        var baseDir = AppContext.BaseDirectory;
        var rulesets = new JsonRulesetProvider(Path.Combine(baseDir, "rulesets"));
        var calc = new TaxCalculationService(rulesets, new EmploymentTaxCalculator(), new ForfettarioTaxCalculator());
        var surtax = new SurtaxJsonProvider(Path.Combine(baseDir, "addizionali"));
        repo = new InMemoryComputationRepository();
        return new MainWindowViewModel(calc, repo, new InMemoryProfileStore(), exporter, surtax);
    }
}

internal sealed class InMemoryComputationRepository : ITaxComputationRepository
{
    public List<ComputationRecord> Items { get; } = new();
    private int _nextId = 1;

    public Task AddAsync(ComputationRecord record, CancellationToken ct = default)
    {
        record.Id = _nextId++;
        Items.Add(record);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ComputationRecord>> GetRecentAsync(int take = 100, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ComputationRecord>>(
            Items.OrderByDescending(x => x.CreatedUtc).ThenByDescending(x => x.Id).Take(take).ToList());

    public Task<ComputationRecord?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

    public Task DeleteAsync(int id, CancellationToken ct = default)
    {
        Items.RemoveAll(x => x.Id == id);
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        Items.Clear();
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryProfileStore : IProfileStore
{
    private readonly List<SavedProfile> _items = new();

    public string StorePath => "(memoria)";

    public Task<IReadOnlyList<SavedProfile>> LoadAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SavedProfile>>(_items.ToList());

    public Task SaveAsync(SavedProfile profile, CancellationToken ct = default)
    {
        _items.RemoveAll(p => p.Id == profile.Id);
        _items.Add(profile);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _items.RemoveAll(p => p.Id == id);
        return Task.CompletedTask;
    }
}

/// <summary>Esportatore che fallisce sempre, per simulare un percorso non scrivibile / I/O in errore.</summary>
internal sealed class ThrowingReportExporter : IReportExporter
{
    public Task ExportTextAsync(TaxResult result, string filePath, CancellationToken ct = default) =>
        throw new IOException("destinazione non scrivibile (simulata)");

    public Task ExportJsonAsync(TaxResult result, string filePath, CancellationToken ct = default) =>
        throw new IOException("destinazione non scrivibile (simulata)");
}
