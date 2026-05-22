using ProductApi.Application.DTOs;
using ProductApi.Domain.Entities;

namespace ProductApi.Application.Mappings;

/// <summary>
/// Mapeos manuales entre entidades de dominio y DTOs.
/// Evita dependencias externas (AutoMapper) manteniendo control total.
/// </summary>
public static class ProductMapper
{
    public static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Price,
        product.Quantity,
        product.RowVersion,
        product.CreatedAt,
        product.UpdatedAt
    );

    public static Product ToEntity(this CreateProductDto dto) => new()
    {
        Name = dto.Name,
        Price = dto.Price,
        Quantity = dto.Quantity
    };
}
