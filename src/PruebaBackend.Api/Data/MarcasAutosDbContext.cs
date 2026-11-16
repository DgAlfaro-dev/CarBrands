using Microsoft.EntityFrameworkCore;
using PruebaBackend.Api.Data.Configurations;
using PruebaBackend.Api.Entities;

namespace PruebaBackend.Api.Data;

/// <summary>
/// Entity Framework Core context for the brands catalog on PostgreSQL. It is
/// injected via <c>AddDbContext</c> and its options come from application
/// configuration (environment variables outside local development).
/// </summary>
public class MarcasAutosDbContext : DbContext
{
    public MarcasAutosDbContext(DbContextOptions<MarcasAutosDbContext> options)
        : base(options)
    {
    }

    public DbSet<MarcaAuto> MarcasAutos => Set<MarcaAuto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new MarcaAutoConfiguration());
    }
}
