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
    public async Task Crud_CreateUpdateAndDelete_ReturnsExpectedResponses()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/marcas-autos", new MarcaAutoRequest { Nombre = "  Honda  " });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);
        var created = await createResponse.Content.ReadFromJsonAsync<MarcaAutoResponse>();
        Assert.NotNull(created);
        Assert.Equal("Honda", created.Nombre);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/marcas-autos/{created.Id}", new MarcaAutoRequest { Nombre = "Honda Motor" });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MarcaAutoResponse>();
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Honda Motor", updated.Nombre);

        var deleteResponse = await _client.DeleteAsync($"/api/marcas-autos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/marcas-autos/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/marcas-autos", new MarcaAutoRequest { Nombre = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task UpdateOrDelete_UnknownId_ReturnsNotFound(string method)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/marcas-autos/9999");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new MarcaAutoRequest { Nombre = "Honda" });
        }

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetByNombre_CaseInsensitiveContains_ReturnsOnlyMatches()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=TOY");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Toyota", result.Data[0].Nombre);
        Assert.Equal(1, result.CurrentPage);
    }

    [Fact]
    public async Task GetByNombre_WithoutMatches_ReturnsEmptyData()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=marca-inexistente");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(0, result.TotalItems);
    }

    [Fact]
    public async Task GetPaginated_FirstPage_ReturnsPageWithTotalHeaders()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Toyota", result.Data[0].Nombre);
        Assert.Equal("Ford", result.Data[1].Nombre);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetPaginated_SecondPage_ReturnsRemainingBrand()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal("Chevrolet", result.Data[0].Nombre);
        Assert.Equal(2, result.CurrentPage);
    }

    [Fact]
    public async Task GetPaginated_BeyondLastPage_ReturnsEmptyButKeepsHeaders()
    {
        var response = await _client.GetAsync("/api/marcas-autos?page=50&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Empty(result.Data);
        Assert.Equal(3, result.TotalItems);
    }

    [Fact]
    public async Task GetPaginated_WithFilter_CountsOnlyMatchingRows()
    {
        var response = await _client.GetAsync("/api/marcas-autos?nombre=toy&page=1&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<MarcaAutoResponse>>();
        Assert.NotNull(result);
        Assert.Single(result.Data);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(1, result.TotalPages);
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
