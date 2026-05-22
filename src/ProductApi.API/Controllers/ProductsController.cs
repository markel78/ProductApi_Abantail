using Microsoft.AspNetCore.Mvc;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;

namespace ProductApi.API.Controllers;

/// <summary>
/// Controlador de productos. Expone los endpoints CRUD bajo /api/v1/products.
/// Principio S de SOLID: sólo se ocupa de traducir HTTP ↔ use-cases.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService service, ILogger<ProductsController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Obtiene la lista paginada de productos con filtro y ordenación opcionales.
    /// </summary>
    /// <param name="query">Parámetros de paginación, filtrado y ordenación.</param>
    /// <param name="cancellationToken"></param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] ProductQueryParams query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/products called.");
        var result = await _service.GetAllAsync(query, cancellationToken);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Obtiene un producto por su identificador.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET /api/v1/products/{Id} called.", id);
        var product = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Crea un nuevo producto.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST /api/v1/products called.");
        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Reemplaza completamente un producto (requiere RowVersion para concurrencia).
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("PUT /api/v1/products/{Id} called.", id);
        var updated = await _service.UpdateAsync(id, dto, cancellationToken);
        return Ok(updated);
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Actualiza parcialmente un producto. Solo se modifican los campos informados.
    /// Requiere RowVersion para control de concurrencia optimista.
    /// </summary>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Patch(
        int id,
        [FromBody] PatchProductDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("PATCH /api/v1/products/{Id} called.", id);
        var patched = await _service.PatchAsync(id, dto, cancellationToken);
        return Ok(patched);
    }

    // ──────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Elimina un producto por su identificador.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("DELETE /api/v1/products/{Id} called.", id);
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
