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

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Data.Count);
        Assert.Contains(result.Data, m => m.Nombre == "Toyota");
        Assert.Contains(result.Data, m => m.Nombre == "Ford");
        Assert.Contains(result.Data, m => m.Nombre == "Chevrolet");
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task GetRoot_RedirectsToSwagger()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("/swagger", response.RequestMessage?.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task GetUnknownRoute_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/no-existe");

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

    [Fact]
    public async Task GetSwaggerDocument_DocumentsLookupFiltersAndPagination()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var schema = await response.Content.ReadAsStringAsync();

        // The lookup endpoint and the query parameters are part of the contract.
        Assert.Contains("/api/marcas-autos/{id}", schema);
        Assert.Contains("\"post\"", schema);
        Assert.Contains("\"put\"", schema);
        Assert.Contains("\"delete\"", schema);
        Assert.Contains("\"nombre\"", schema);
        Assert.Contains("\"page\"", schema);
        Assert.Contains("\"pageSize\"", schema);
        Assert.Contains("\"currentPage\"", schema);
        Assert.Contains("\"data\"", schema);

        // The usage guide (filters and pagination metadata) reaches the document
        // through the XML remarks of the operation and the API description.
        Assert.Contains("totalItems", schema);
        Assert.Contains("sin distinguir mayúsculas", schema);
    }
}
