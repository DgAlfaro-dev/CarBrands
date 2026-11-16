using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PruebaBackend.Api.Dtos;
using PruebaBackend.Api.Tests.Infrastructure;

namespace PruebaBackend.Api.Tests.Integration;

/// <summary>
/// Integration tests for the lookup, search and pagination surface:
/// GET /api/marcas-autos with query parameters and GET /api/marcas-autos/{id}.
/// </summary>
public class MarcasAutosQueryTests : IClassFixture<MarcasAutosApiFactory>
{
    private readonly HttpClient _client;

    public MarcasAutosQueryTests(MarcasAutosApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsThatBrand()
    {
        var response = await _client.GetAsync("/api/marcas-autos/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marca = await response.Content.ReadFromJsonAsync<MarcaAutoResponse>();
        Assert.NotNull(marca);
        Assert.Equal(1, marca.Id);
        Assert.Equal("Toyota", marca.Nombre);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync("/api/marcas-autos/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        Assert.Equal("Marca no encontrada", problem.Title);
    }

    [Fact]
    public async Task GetByNombre_CaseInsensitiveContains_ReturnsOnlyMatches()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=TOY");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Single(marcas);
        Assert.Equal("Toyota", marcas[0].Nombre);
    }

    [Fact]
    public async Task GetByNombre_WithoutMatches_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=marca-inexistente");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Empty(marcas);
    }

    [Fact]
    public async Task GetPaginated_FirstPage_ReturnsPageWithTotalHeaders()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Equal(2, marcas.Count);
        Assert.Equal("Toyota", marcas[0].Nombre);
        Assert.Equal("Ford", marcas[1].Nombre);

        Assert.Equal("3", Assert.Single(response.Headers.GetValues("X-Total-Count")));
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Page")));
        Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Page-Size")));
        Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Total-Pages")));
    }

    [Fact]
    public async Task GetPaginated_SecondPage_ReturnsRemainingBrand()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Single(marcas);
        Assert.Equal("Chevrolet", marcas[0].Nombre);
        Assert.Equal("2", Assert.Single(response.Headers.GetValues("X-Page")));
    }

    [Fact]
    public async Task GetPaginated_BeyondLastPage_ReturnsEmptyButKeepsHeaders()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=50&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Empty(marcas);
        Assert.Equal("3", Assert.Single(response.Headers.GetValues("X-Total-Count")));
    }

    [Fact]
    public async Task GetPaginated_WithFilter_CountsOnlyMatchingRows()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=toy&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var marcas = await response.Content.ReadFromJsonAsync<List<MarcaAutoResponse>>();
        Assert.NotNull(marcas);
        Assert.Single(marcas);
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Total-Count")));
        Assert.Equal("1", Assert.Single(response.Headers.GetValues("X-Total-Pages")));
    }

    [Fact]
    public async Task GetPaginated_PageBelowOne_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("page"));
    }

    [Fact]
    public async Task GetPaginated_PageSizeAboveLimit_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/marcas-autos?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.True(problem.Errors.ContainsKey("pageSize"));
    }

    [Fact]
    public async Task GetPaginated_NonNumericPage_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
