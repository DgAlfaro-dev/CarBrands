using Microsoft.EntityFrameworkCore;
using PruebaBackend.Api.Data;
using PruebaBackend.Api.Entities;
using PruebaBackend.Api.Tests.Infrastructure;

namespace PruebaBackend.Api.Tests.Data;

/// <summary>
/// Tests for the data seed mechanism (requirement: table with at least 3 brands).
/// </summary>
public class MarcasAutosSeedTests
{
    // Shared query: creates an isolated context, reads the brands and disposes.
    private static async Task<List<MarcaAuto>> GetBrandsAsync(bool seedDefaultData = true)
    {
        await using var context = MarcasAutosContextFactory.Create(seedDefaultData);
        return await context.MarcasAutos.AsNoTracking().ToListAsync();
    }

    [Fact]
    public async Task SeededDatabase_ContainsAtLeastThreeBrands()
    {
        var marcas = await GetBrandsAsync();

        Assert.True(marcas.Count >= 3, $"Expected at least 3 brands but found {marcas.Count}.");
        Assert.All(marcas, m => Assert.False(string.IsNullOrWhiteSpace(m.Nombre)));
    }

    [Fact]
    public async Task SeededBrands_HaveUniquePositiveIds()
    {
        var marcas = await GetBrandsAsync();

        Assert.Equal(marcas.Count, marcas.Select(m => m.Id).Distinct().Count());
        Assert.All(marcas, m => Assert.True(m.Id > 0));
    }
}
