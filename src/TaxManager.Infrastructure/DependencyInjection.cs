using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxManager.Application.Abstractions;
using TaxManager.Infrastructure.Persistence;
using TaxManager.Infrastructure.Rulesets;
using TaxManager.Infrastructure.Surtaxes;

namespace TaxManager.Infrastructure;

/// <summary>Registrazione dei servizi di infrastruttura (persistenza relazionale + disco + ruleset).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddTaxManagerInfrastructure(
        this IServiceCollection services,
        TaxManagerStorageOptions? options = null)
    {
        var opts = options ?? TaxManagerStorageOptions.CreateDefault();
        Directory.CreateDirectory(opts.DataDirectory);

        services.AddSingleton(opts);

        // Persistenza relazionale (SQLite) tramite factory: un contesto per operazione.
        services.AddDbContextFactory<TaxManagerDbContext>(o => o.UseSqlite($"Data Source={opts.DatabasePath}"));
        services.AddSingleton<ITaxComputationRepository, SqliteTaxComputationRepository>();

        // Persistenza su disco (JSON).
        services.AddSingleton<IProfileStore>(_ => new JsonProfileStore(opts.ProfilesPath));
        services.AddSingleton<IReportExporter, FileReportExporter>();

        // Ruleset (JSON su disco + fallback in codice).
        services.AddSingleton<IRulesetProvider>(_ => new JsonRulesetProvider(opts.RulesetsDirectory));

        // Addizionali regionali/comunali (JSON su disco).
        services.AddSingleton<ISurtaxProvider>(_ => new SurtaxJsonProvider(opts.AddizionaliDirectory));

        return services;
    }

    /// <summary>Crea il database e lo schema se non esistono. Da chiamare all'avvio.</summary>
    public static void EnsureTaxManagerDatabaseCreated(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<TaxManagerDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
    }
}
