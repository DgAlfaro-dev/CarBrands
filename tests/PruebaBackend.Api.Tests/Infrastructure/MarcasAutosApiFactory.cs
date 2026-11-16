using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PruebaBackend.Api.Data;

namespace PruebaBackend.Api.Tests.Infrastructure;

/// <summary>
/// Boots the real API (the whole of Program.cs: middlewares, DI, ProblemDetails,
/// Swagger and auto-migration) replacing PostgreSQL with an in-memory database
/// so integration tests do not depend on an external server.
/// </summary>
public sealed class MarcasAutosApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // The DbContext registered with Npgsql in Program.cs is removed and
            // an equivalent registration with the in-memory provider is added.
            // The connection string from appsettings.json is never used.
            var optionsDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(DbContextOptions<MarcasAutosDbContext>))
                .ToList();

            foreach (var descriptor in optionsDescriptors)
            {
                services.Remove(descriptor);
            }

            // The name is generated ONCE outside the lambda: if it were inside,
            // every options resolution would create a different, isolated database.
            var databaseName = $"marcas-autos-api-{Guid.NewGuid():N}";

            services.AddDbContext<MarcasAutosDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // The in-memory provider creates the schema lazily and does NOT apply
        // the HasData seed on its own: EnsureCreated is forced once so the test
        // API starts with the 3 brands, just like the migration would do on
        // PostgreSQL in production.
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MarcasAutosDbContext>();
        dbContext.Database.EnsureCreated();

        return host;
    }
}
