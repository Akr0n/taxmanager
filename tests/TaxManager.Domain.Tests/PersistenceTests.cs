using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaxManager.Application.Models;
using TaxManager.Application.Services;
using TaxManager.Domain;
using TaxManager.Domain.Engine;
using TaxManager.Domain.Inputs;
using TaxManager.Infrastructure.Persistence;

namespace TaxManager.Domain.Tests;

/// <summary>
/// Robustezza della persistenza: round-trip dei profili e dello storico, e degradazione sicura
/// (file/JSON corrotti non devono far crashare l'app).
/// </summary>
public class PersistenceTests
{
    private static string TempFile(string ext) =>
        Path.Combine(Path.GetTempPath(), $"tm-test-{Guid.NewGuid():N}.{ext}");

    [Fact]
    public async Task JsonProfileStore_roundtrips_a_profile()
    {
        var path = TempFile("json");
        try
        {
            var store = new JsonProfileStore(path);
            await store.SaveAsync(new SavedProfile { Name = "Mario", GrossAnnualSalary = 25_000m });

            var loaded = await store.LoadAllAsync();

            Assert.Single(loaded);
            Assert.Equal("Mario", loaded[0].Name);
            Assert.Equal(25_000m, loaded[0].GrossAnnualSalary);
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public async Task JsonProfileStore_returns_empty_on_corrupt_file()
    {
        var path = TempFile("json");
        await File.WriteAllTextAsync(path, "{ questo non è json valido ]");
        try
        {
            var store = new JsonProfileStore(path);

            var loaded = await store.LoadAllAsync(); // non deve lanciare

            Assert.Empty(loaded);
        }
        finally { TryDelete(path); }
    }

    [Fact]
    public void ReadResult_returns_null_on_corrupt_json()
    {
        var record = new ComputationRecord { ResultJson = "{ json rotto" };

        var result = ComputationRecordFactory.ReadResult(record); // non deve lanciare

        Assert.Null(result);
    }

    [Fact]
    public void ComputationRecordFactory_roundtrips_taxresult_from_history()
    {
        var original = new EmploymentTaxCalculator().Calculate(
            new EmploymentIncomeInput { GrossAnnualSalary = 30_000m, EmploymentDays = 365 },
            SampleRulesets.Year2025());

        var record = ComputationRecordFactory.Create(original, "profilo", new { dummy = 1 });
        var back = ComputationRecordFactory.ReadResult(record);

        Assert.NotNull(back);
        Assert.Equal(original.Category, back!.Category);
        Assert.Equal(original.NetAnnualIncome, back.NetAnnualIncome);
        Assert.Equal(original.TotalTaxes, back.TotalTaxes);
        Assert.Equal(original.Lines.Count, back.Lines.Count);
    }

    [Fact]
    public async Task SqliteRepository_supports_add_get_delete_clear()
    {
        var dbPath = TempFile("db");
        try
        {
            var factory = new TestDbContextFactory(dbPath);
            using (var db = factory.CreateDbContext()) db.Database.EnsureCreated();
            var repo = new SqliteTaxComputationRepository(factory);

            await repo.AddAsync(new ComputationRecord
            {
                CreatedUtc = DateTime.UtcNow,
                Category = TaxpayerCategory.Forfettario,
                Year = 2025,
                GrossIncome = 50_000m,
            });

            var recent = await repo.GetRecentAsync(10);
            Assert.Single(recent);
            var id = recent[0].Id;

            var byId = await repo.GetByIdAsync(id);
            Assert.NotNull(byId);

            await repo.DeleteAsync(id);
            Assert.Empty(await repo.GetRecentAsync(10));

            await repo.AddAsync(new ComputationRecord { CreatedUtc = DateTime.UtcNow });
            await repo.ClearAsync();
            Assert.Empty(await repo.GetRecentAsync(10));
        }
        finally
        {
            SqliteConnectionPoolCleanup();
            TryDelete(dbPath);
        }
    }

    private static void SqliteConnectionPoolCleanup()
    {
        try { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); }
        catch { /* best effort */ }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best effort cleanup */ }
    }

    private sealed class TestDbContextFactory : IDbContextFactory<TaxManagerDbContext>
    {
        private readonly DbContextOptions<TaxManagerDbContext> _options;

        public TestDbContextFactory(string dbPath) =>
            _options = new DbContextOptionsBuilder<TaxManagerDbContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

        public TaxManagerDbContext CreateDbContext() => new(_options);
    }
}
