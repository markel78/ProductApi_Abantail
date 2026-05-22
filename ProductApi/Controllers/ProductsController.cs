using Microsoft.AspNetCore.Mvc;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;

namespace ProductApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _service;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductService service, ILogger<ProductsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // GET /api/v1/products?page=1&pageSize=10&nameFilter=abc&sortBy=name&sortDescending=false
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<ProductResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nameFilter = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation("GET /products");
        var result = await _service.GetAllAsync(page, pageSize, nameFilter, sortBy, sortDescending, ct);
        return Ok(result);
    }

    // GET /api/v1/products/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        _logger.LogInformation("GET /products/{Id}", id);
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    // POST /api/v1/products
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        _logger.LogInformation("POST /products");
        var created = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // PUT /api/v1/products/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        _logger.LogInformation("PUT /products/{Id}", id);
        var updated = await _service.UpdateAsync(id, dto, ct);
        return Ok(updated);
    }

    // PATCH /api/v1/products/5
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ProductResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchProductDto dto, CancellationToken ct)
    {
        _logger.LogInformation("PATCH /products/{Id}", id);
        var patched = await _service.PatchAsync(id, dto, ct);
        return Ok(patched);
    }

    // DELETE /api/v1/products/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        _logger.LogInformation("DELETE /products/{Id}", id);
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}