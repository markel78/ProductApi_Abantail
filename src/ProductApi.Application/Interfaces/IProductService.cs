using ProductApi.Application.DTOs;

namespace ProductApi.Application.Interfaces;

/// <summary>
/// Contrato del servicio de productos (use-cases).
/// Principio S de SOLID: esta interfaz encapsula únicamente la lógica de negocio de productos.
/// </summary>
public interface IProductService
{
    Task<PagedResult<ProductDto>> GetAllAsync(ProductQueryParams query, CancellationToken cancellationToken = default);
    Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default);
    Task<ProductDto> PatchAsync(int id, PatchProductDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
