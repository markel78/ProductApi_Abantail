using Microsoft.Extensions.Logging;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Application.Services;

// SRP: este servicio solo orquesta los casos de uso, no persiste ni serializa
// OCP: para añadir funcionalidad nueva solo añado métodos, no toco lo existente
// Patrón Service Layer: la lógica de negocio vive aquí, fuera del controlador
// ToDto: convierte la entidad interna en lo que el cliente ve
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repo;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repo, ILogger<ProductService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
        int page, int pageSize, string? nameFilter,
        string? sortBy, bool sortDescending, CancellationToken ct = default)
    {
        _logger.LogInformation("GetAll --> page={Page} size={Size} filter={Filter}",
            page, pageSize, nameFilter);

        var (items, total) = await _repo.GetAllAsync(
            page, pageSize, nameFilter, sortBy, sortDescending, ct);

        return new PagedResultDto<ProductResponseDto>(
            Items: items.Select(ToDto),
            TotalCount: total,
            Page: page,
            PageSize: pageSize,
            TotalPages: (int)Math.Ceiling(total / (double)pageSize)
        );
    }

    public async Task<ProductResponseDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("GetById --> id={Id}", id);
        var product = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return ToDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Create --> name={Name}", dto.Name);
        var product = new Product
        {
            Name = dto.Name,
            Price = dto.Price,
            Quantity = dto.Quantity
        };
        var created = await _repo.CreateAsync(product, ct);
        return ToDto(created);
    }

    public async Task<ProductResponseDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Update --> id={Id}", id);
        var existing = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        existing.Name = dto.Name;
        existing.Price = dto.Price;
        existing.Quantity = dto.Quantity;
        existing.RowVersion = Convert.FromBase64String(dto.RowVersion);

        var updated = await _repo.UpdateAsync(existing, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return ToDto(updated);
    }

    public async Task<ProductResponseDto> PatchAsync(int id, PatchProductDto dto, CancellationToken ct = default)
    {
        _logger.LogInformation("Patch --> id={Id}", id);
        var existing = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        if (dto.Name is not null) existing.Name = dto.Name;
        if (dto.Price is not null) existing.Price = dto.Price.Value;
        if (dto.Quantity is not null) existing.Quantity = dto.Quantity.Value;
        existing.RowVersion = Convert.FromBase64String(dto.RowVersion);

        var updated = await _repo.UpdateAsync(existing, ct)
            ?? throw new NotFoundException(nameof(Product), id);
        return ToDto(updated);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Delete --> id={Id}", id);
        var deleted = await _repo.DeleteAsync(id, ct);
        if (!deleted) throw new NotFoundException(nameof(Product), id);
    }

    public async Task<IEnumerable<ProductResponseDto>> SearchByNameAsync(string name, CancellationToken ct = default)
    {
        _logger.LogInformation("SearchByName --> name={Name}", name);
        var (items, _) = await _repo.GetAllAsync(1, int.MaxValue, name, null, false, ct);
        return items.Select(ToDto);
    }

    private static ProductResponseDto ToDto(Product p) => new(
        Id: p.Id,
        Name: p.Name,
        Price: p.Price,
        Quantity: p.Quantity,
        RowVersion: Convert.ToBase64String(p.RowVersion)
    );
}