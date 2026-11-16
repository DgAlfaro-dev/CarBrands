using System.Net;
using System.Net.Http.Json;
using PruebaBackend.Api.Dtos;
using PruebaBackend.Api.Tests.Infrastructure;

namespace PruebaBackend.Api.Tests.Integration;

/// <summary>
/// Integration tests: they boot the real API (the full Program.cs) and consume
/// it over HTTP, verifying the contract exactly as a client would see it.
/// </summary>
public class MarcasAutosEndpointTests : IClassFixture<MarcasAutosApiFactory>
{
    private readonly HttpClient _client;

    public MarcasAutosEndpointTests(MarcasAutosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMarcasAutos_ReturnsOkWithSeededBrands()
    {
        var response = await _client.GetAsync("/api/marcas-autos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Equal(3, marcas.Count);
        Assert.Contains(marcas, m => m.Nombre == "Toyota");
        Assert.Contains(marcas, m => m.Nombre == "Ford");
        Assert.Contains(marcas, m => m.Nombre == "Chevrolet");
    }

    // The root path is intentionally NOT an endpoint: only /api/marcas-autos
    // exists, so both the bare root and unknown routes must answer 404.
    [Theory]
    [InlineData("/")]
    [InlineData("/api/no-existe")]
    public async Task GetUnknownRoute_ReturnsNotFound(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerDocument_ReturnsOkWithApiSchema()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schema = await response.Content.ReadAsStringAsync();
        Assert.Contains("MarcasAutos", schema);
    }
}
