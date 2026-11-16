using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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

    private sealed class MissingConnectionStringApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Invalidates the appsettings.json value: host settings take
            // precedence over configuration files in the configuration chain.
            builder.UseSetting("ConnectionStrings:Postgres", string.Empty);
        }
    }
}
