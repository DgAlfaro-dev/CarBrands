using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PruebaBackend.Api.Data;
using PruebaBackend.Api.Dtos;

namespace PruebaBackend.Api.Controllers;

/// <summary>
/// Read-only endpoint for the car brands catalog. An explicit route is used
/// instead of <c>[controller]</c> so the HTTP contract stays stable even if
/// the class is renamed.
/// </summary>
[ApiController]
[Route("api/marcas-autos")]
public class MarcasAutosController : ControllerBase
{
    private readonly MarcasAutosDbContext _dbContext;

    // The test requires a single table: injecting the DbContext directly keeps
    // the scope small (no repositories/services without real consumers) and
    // lets the endpoint be tested against an in-memory context.
    public MarcasAutosController(MarcasAutosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns all car brands ordered by id.
    /// </summary>
    /// <response code="200">List of brands (empty when there are no rows).</response>
    /// <response code="500">Unexpected error while querying the database.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MarcaAutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MarcaAutoResponse>>> GetAll(CancellationToken cancellationToken)
    {
        // AsNoTracking: read-only query, no need to track changes.
        // Explicit order by Id keeps the response deterministic.
        var marcas = await _dbContext.MarcasAutos
            .AsNoTracking()
            .OrderBy(m => m.Id)
            .Select(m => new MarcaAutoResponse(m.Id, m.Nombre))
            .ToListAsync(cancellationToken);

        return Ok(marcas);
    }
}
