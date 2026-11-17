using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using PruebaBackend.Api.Data;
using PruebaBackend.Api.Dtos;
using PruebaBackend.Api.Entities;

namespace PruebaBackend.Api.Controllers;

/// <summary>
/// CRUD endpoints for the car brands catalog, including a searchable,
/// paginated list and lookup by id.
/// </summary>
[ApiController]
[Route("api/marcas-autos")]
public class MarcasAutosController : ControllerBase
{
    /// <summary>Page size applied when only <c>page</c> is sent.</summary>
    private const int DefaultPageSize = 10;

    /// <summary>Upper bound accepted for <c>pageSize</c>.</summary>
    private const int MaxPageSize = 100;

    private readonly MarcasAutosDbContext _dbContext;

    // The test requires a single table: injecting the DbContext directly keeps
    // the scope small (no repositories/services without real consumers) and
    // lets the endpoint be tested against an in-memory context.
    public MarcasAutosController(MarcasAutosDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Devuelve las marcas de autos ordenadas por id, con búsqueda por nombre
    /// y paginación opcionales.
    /// </summary>
    /// <remarks>
    /// ## Parámetros de consulta (todos opcionales)
    ///
    /// | Parámetro | Ejemplo | Descripción |
    /// |---|---|---|
    /// | `nombre` | `/api/marcas-autos?nombre=toy` | Busca por nombre: coincidencia **contenida** y **sin distinguir mayúsculas** (`toy` encuentra `Toyota`). Se ignora si viene vacío. |
    /// | `page` | `/api/marcas-autos?page=2` | Página a devolver, empezando en **1**. |
    /// | `pageSize` | `/api/marcas-autos?pageSize=10` | Filas por página, entre **1 y 100**. |
    ///
    /// La respuesta siempre es un objeto paginado con `data`, `currentPage`,
    /// `pageSize`, `totalItems` y `totalPages`. Si omites `page` o `pageSize`,
    /// se usan `page = 1` y `pageSize = 10`.
    ///
    /// ## Metadatos de paginación en la respuesta
    ///
    /// | Campo | Descripción |
    /// |---|---|
    /// | `data` | Marcas de la página actual. |
    /// | `currentPage` | Número de página devuelto, empezando en 1. |
    /// | `pageSize` | Máximo de marcas por página (`limitPerPage`). |
    /// | `totalItems` | Total de resultados después de aplicar el filtro. |
    /// | `totalPages` | Total de páginas disponibles. |
    ///
    /// ## Ejemplos
    ///
    /// ```http
    /// GET /api/marcas-autos?nombre=ford
    /// → {"data":[{"id":2,"nombre":"Ford"}],"currentPage":1,"pageSize":10,"totalItems":1,"totalPages":1}
    /// GET /api/marcas-autos?page=2&amp;pageSize=2
    /// → {"data":[{"id":3,"nombre":"Chevrolet"}],"currentPage":2,"pageSize":2,"totalItems":3,"totalPages":2}
    /// ```
    /// </remarks>
    /// <param name="nombre">Filtro por nombre (parcial, sin distinguir mayúsculas).</param>
    /// <param name="page">Página a devolver, mínima 1.</param>
    /// <param name="pageSize">Filas por página, entre 1 y 100.</param>
    /// <param name="cancellationToken">Cancela la consulta si la petición se aborta.</param>
    /// <response code="200">Objeto paginado con las marcas y sus metadatos.</response>
    /// <response code="400">`page` o `pageSize` fuera de rango; el detalle está en `errors`.</response>
    /// <response code="500">Error inesperado al consultar la base de datos.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<MarcaAutoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResponse<MarcaAutoResponse>>> GetAll(
        [FromQuery] string? nombre,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var errores = new Dictionary<string, string[]>();

        if (page is not null && page < 1)
        {
            errores["page"] = new[] { "El parámetro 'page' debe ser un entero mayor o igual a 1." };
        }

        if (pageSize is not null && (pageSize < 1 || pageSize > MaxPageSize))
        {
            errores["pageSize"] = new[] { $"El parámetro 'pageSize' debe estar entre 1 y {MaxPageSize}." };
        }

        if (errores.Count > 0)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (campo, mensajes) in errores)
            {
                foreach (var mensaje in mensajes)
                {
                    modelState.AddModelError(campo, mensaje);
                }
            }

            return ValidationProblem(modelState);
        }

        var query = _dbContext.MarcasAutos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            // Lowercasing both sides makes the match case-insensitive and still
            // translates to SQL (lower(nombre) LIKE ...), so it also works on
            // the in-memory test provider.
            var filtro = nombre.Trim().ToLowerInvariant();
            query = query.Where(m => m.Nombre.ToLower().Contains(filtro));
        }

        // Explicit order by Id keeps the response (and therefore every page)
        // deterministic.
        query = query.OrderBy(m => m.Id);

        var total = await query.CountAsync(cancellationToken);

        var pagina = page ?? 1;
        var tamano = pageSize ?? DefaultPageSize;
        var totalPaginas = (int)Math.Ceiling(total / (double)tamano);
        var offset = (long)(pagina - 1) * tamano;
        var marcas = offset > int.MaxValue
            ? new List<MarcaAutoResponse>()
            : await query
                .Skip((int)offset)
                .Take(tamano)
                .Select(m => new MarcaAutoResponse(m.Id, m.Nombre))
                .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<MarcaAutoResponse>(
            marcas, pagina, tamano, total, totalPaginas));
    }

    /// <summary>
    /// Devuelve una sola marca de autos por su identificador.
    /// </summary>
    /// <remarks>
    /// **Ejemplo**
    ///
    /// ```http
    /// GET /api/marcas-autos/1
    /// → 200 {"id":1,"nombre":"Toyota"}
    /// → 404 (ProblemDetails) si el id no existe
    /// ```
    /// </remarks>
    /// <param name="id">Identificador numérico de la marca.</param>
    /// <param name="cancellationToken">Cancela la consulta si la petición se aborta.</param>
    /// <response code="200">La marca solicitada.</response>
    /// <response code="404">No existe ninguna marca con ese identificador.</response>
    /// <response code="500">Error inesperado al consultar la base de datos.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MarcaAutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarcaAutoResponse>> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var marca = await _dbContext.MarcasAutos
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (marca is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Marca no encontrada",
                detail: $"No existe ninguna marca de autos con id {id}.",
                type: "https://tools.ietf.org/html/rfc9110#section-15.5.5");
        }

        return Ok(new MarcaAutoResponse(marca.Id, marca.Nombre));
    }

    /// <summary>Crea una marca de autos.</summary>
    /// <remarks>
    /// Envía un cuerpo JSON con `nombre`. El nombre es obligatorio y admite
    /// hasta 100 caracteres. La respuesta incluye la ruta de la marca creada.
    ///
    /// ```json
    /// { "nombre": "Honda" }
    /// ```
    /// </remarks>
    /// <param name="request">Nombre de la marca que se creará.</param>
    /// <param name="cancellationToken">Cancela la operación si la petición se aborta.</param>
    /// <response code="201">Marca creada; el encabezado `Location` apunta a GET por id.</response>
    /// <response code="400">El nombre está vacío o excede los 100 caracteres.</response>
    /// <response code="500">Error inesperado al guardar en la base de datos.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MarcaAutoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarcaAutoResponse>> Create(
        [FromBody] MarcaAutoRequest request,
        CancellationToken cancellationToken)
    {
        var marca = new MarcaAuto { Nombre = request.Nombre.Trim() };
        _dbContext.MarcasAutos.Add(marca);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new MarcaAutoResponse(marca.Id, marca.Nombre);
        return CreatedAtAction(nameof(GetById), new { id = marca.Id }, response);
    }

    /// <summary>Actualiza el nombre de una marca existente.</summary>
    /// <remarks>
    /// Envía el nuevo nombre en el cuerpo JSON. El id se indica en la ruta y
    /// no se incluye en el cuerpo.
    ///
    /// ```json
    /// { "nombre": "Honda Motor" }
    /// ```
    /// </remarks>
    /// <param name="id">Identificador de la marca que se actualizará.</param>
    /// <param name="request">Nuevo nombre de la marca.</param>
    /// <param name="cancellationToken">Cancela la operación si la petición se aborta.</param>
    /// <response code="200">Marca actualizada.</response>
    /// <response code="400">El nombre está vacío o excede los 100 caracteres.</response>
    /// <response code="404">No existe una marca con ese id.</response>
    /// <response code="500">Error inesperado al guardar en la base de datos.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(MarcaAutoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<MarcaAutoResponse>> Update(
        [FromRoute] int id,
        [FromBody] MarcaAutoRequest request,
        CancellationToken cancellationToken)
    {
        var marca = await _dbContext.MarcasAutos
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (marca is null)
        {
            return CreateNotFoundProblem(id);
        }

        marca.Nombre = request.Nombre.Trim();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new MarcaAutoResponse(marca.Id, marca.Nombre));
    }

    /// <summary>Elimina una marca existente.</summary>
    /// <param name="id">Identificador de la marca que se eliminará.</param>
    /// <param name="cancellationToken">Cancela la operación si la petición se aborta.</param>
    /// <response code="204">Marca eliminada.</response>
    /// <response code="404">No existe una marca con ese id.</response>
    /// <response code="500">Error inesperado al guardar en la base de datos.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var marca = await _dbContext.MarcasAutos
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (marca is null)
        {
            return CreateNotFoundProblem(id);
        }

        _dbContext.MarcasAutos.Remove(marca);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private NotFoundObjectResult CreateNotFoundProblem(int id)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Marca no encontrada",
            Detail = $"No existe ninguna marca de autos con id {id}.",
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5"
        };

        return NotFound(problem);
    }
}
