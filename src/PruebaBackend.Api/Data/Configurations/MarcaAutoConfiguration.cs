using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PruebaBackend.Api.Entities;

namespace PruebaBackend.Api.Data.Configurations;

/// <summary>
/// Mapping for the MarcasAutos table: columns, constraints and data seed.
/// The HasData seed is versioned inside the migration, so any database that
/// runs the migrations ends up populated in a reproducible way.
/// </summary>
public class MarcaAutoConfiguration : IEntityTypeConfiguration<MarcaAuto>
{
    // Fixed ids: HasData uses the primary key to detect rows that already
    // exist, so they must be deterministic to keep the seed idempotent.
    private const int ToyotaId = 1;
    private const int FordId = 2;
    private const int ChevroletId = 3;

    private const int MaxNombreLength = 100;

    public void Configure(EntityTypeBuilder<MarcaAuto> builder)
    {
        builder.ToTable("MarcasAutos");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Nombre)
            .HasMaxLength(MaxNombreLength)
            .IsRequired();

        builder.HasData(
            new MarcaAuto { Id = ToyotaId, Nombre = "Toyota" },
            new MarcaAuto { Id = FordId, Nombre = "Ford" },
            new MarcaAuto { Id = ChevroletId, Nombre = "Chevrolet" });
    }
}
