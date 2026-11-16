using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PruebaBackend.Api.Tests.Infrastructure;

namespace PruebaBackend.Api.Tests.Integration;

/// <summary>
/// Validates the connection string check at startup: missing external
/// configuration must produce an explicit error (best practices §8).
/// </summary>
public class StartupConfigurationTests
{
    [Fact]
    public void Startup_WithoutConnectionString_FailsWithDescriptiveError()
    {
        using var factory = new MissingConnectionStringApiFactory();

        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        // The error may be wrapped by the host, so the whole exception chain
        // is walked before checking the message.
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        Assert.Contains(messages, message => message.Contains("Falta la cadena de conexión"));
    }

    [Fact]
    public async Task Startup_WithRenderStyleUriConnectionString_ServesRequests()
    {
        // A postgres:// URI as the connection string used to crash the app at
        // startup (Npgsql only understands key/value pairs): the API must boot
        // and answer instead.
        using var factory = new RenderUriConnectionStringApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/marcas-autos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed class MissingConnectionStringApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Invalidates the appsettings.json value: host settings take
            // precedence over configuration files in the configuration chain.
            builder.UseSetting("ConnectionStrings:Postgres", string.Empty);
        }
    }

    /// <summary>
    /// Boots the API with a Render-style postgres:// URI as connection string,
    /// reusing the in-memory DbContext replacement from
    /// <see cref="MarcasAutosApiFactory"/> so no real connection is attempted.
    /// </summary>
    private sealed class RenderUriConnectionStringApiFactory : MarcasAutosApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(
                "ConnectionStrings:Postgres",
                "postgres://carbrands:secret123@dpg-abc123-5432.oregon-postgres.render.com/carbrands");

            base.ConfigureWebHost(builder);
        }
    }
}
