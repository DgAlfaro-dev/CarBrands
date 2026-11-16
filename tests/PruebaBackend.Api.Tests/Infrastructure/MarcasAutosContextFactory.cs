using Microsoft.EntityFrameworkCore;
using PruebaBackend.Api.Data;

namespace PruebaBackend.Api.Tests.Infrastructure;

/// <summary>
/// Factory for test contexts backed by an in-memory database (required by the
/// test statement). Each test gets a uniquely named database so no state is
/// shared between tests and the suite can run in parallel.
/// </summary>
internal static class MarcasAutosContextFactory
{
    /// <param name="seedDefaultData">
    /// When <c>true</c> it keeps the <c>HasData</c> seed (3 brands). When
    /// <c>false</c> it empties the table: <c>EnsureCreated</c> always applies
    /// the seed on the in-memory provider, so the rows are deleted after creation.
    /// </param>
    public static MarcasAutosDbContext Create(bool seedDefaultData = true)
    {
        var options = new DbContextOptionsBuilder<MarcasAutosDbContext>()
            .UseInMemoryDatabase($"marcas-autos-{Guid.NewGuid():N}")
            .Options;

        var context = new MarcasAutosDbContext(options);

        // EnsureCreated builds the model (OnModelCreating + HasData) and creates
        // the in-memory schema, leaving the database ready for the test.
        context.Database.EnsureCreated();

        if (!seedDefaultData)
        {
            context.MarcasAutos.RemoveRange(context.MarcasAutos.ToList());
            context.SaveChanges();
        }

        return context;
    }
}
