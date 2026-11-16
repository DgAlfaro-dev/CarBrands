using Microsoft.AspNetCore.Mvc;
using PruebaBackend.Api.Controllers;
using PruebaBackend.Api.Data;
using PruebaBackend.Api.Dtos;
using PruebaBackend.Api.Entities;
using PruebaBackend.Api.Tests.Infrastructure;

namespace PruebaBackend.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the GET /api/marcas-autos endpoint using the in-memory
/// database context required by the test statement.
/// </summary>
public class MarcasAutosControllerTests : IAsyncLifetime
{
    private MarcasAutosDbContext _context = null!;
    private MarcasAutosController _controller = null!;

    // One seeded context + controller per test (xUnit creates a new instance
    // of the class for every test), disposed automatically after it runs.
    public Task InitializeAsync()
    {
        _context = MarcasAutosContextFactory.Create();
        _controller = new MarcasAutosController(_context);
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _context.DisposeAsync().AsTask();

    [Fact]
    public async Task GetAll_WithSeededData_ReturnsOkWithThreeBrands()
    {
        var response = await _controller.GetAll(null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var marcas = Assert.IsType<List<MarcaAutoResponse>>(okResult.Value);
        Assert.Equal(3, marcas.Count);
    }

    [Fact]
    public async Task GetAll_ReturnsSeededBrands_WithIdAndName()
    {
        var response = await _controller.GetAll(null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var marcas = Assert.IsType<List<MarcaAutoResponse>>(okResult.Value);
        Assert.Equal(
            new[] { "Toyota", "Ford", "Chevrolet" },
            marcas.Select(m => m.Nombre).ToArray());
        Assert.All(marcas, m => Assert.True(m.Id > 0));
    }

    [Fact]
    public async Task GetAll_ReturnsBrandsOrderedById_EvenWhenInsertedOutOfOrder()
    {
        // Inserted out of order to prove that the endpoint guarantees the
        // ordering, not the arrival order of the data.
        _context.MarcasAutos.Add(new MarcaAuto { Id = 50, Nombre = "Zeta" });
        _context.MarcasAutos.Add(new MarcaAuto { Id = 4, Nombre = "Alfa" });
        await _context.SaveChangesAsync();

        var response = await _controller.GetAll(null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var marcas = Assert.IsType<List<MarcaAutoResponse>>(okResult.Value);
        var ids = marcas.Select(m => m.Id).ToArray();
        Assert.Equal(ids.OrderBy(id => id).ToArray(), ids);
        Assert.Equal(5, marcas.Count);
    }

    [Fact]
    public async Task GetAll_WhenDatabaseIsEmpty_ReturnsOkWithEmptyList()
    {
        // This case needs an empty table, so it builds its own context
        // instead of the seeded one provided by the fixture.
        await using var context = MarcasAutosContextFactory.Create(seedDefaultData: false);
        var controller = new MarcasAutosController(context);

        var response = await controller.GetAll(null, null, null, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var marcas = Assert.IsType<List<MarcaAutoResponse>>(okResult.Value);
        Assert.Empty(marcas);
    }
}
