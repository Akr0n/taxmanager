using Microsoft.EntityFrameworkCore;
using TaxManager.Application.Models;

namespace TaxManager.Infrastructure.Persistence;

/// <summary>Contesto EF Core (SQLite) per lo storico dei calcoli.</summary>
public sealed class TaxManagerDbContext : DbContext
{
    public TaxManagerDbContext(DbContextOptions<TaxManagerDbContext> options) : base(options)
    {
    }

    public DbSet<ComputationRecord> Computations => Set<ComputationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<ComputationRecord>();
        e.ToTable("Computations");
        e.HasKey(x => x.Id);
        e.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        e.Property(x => x.ProfileName).HasMaxLength(200);
        e.Property(x => x.InputJson).HasColumnType("TEXT");
        e.Property(x => x.ResultJson).HasColumnType("TEXT");
        e.HasIndex(x => x.CreatedUtc);
    }
}
