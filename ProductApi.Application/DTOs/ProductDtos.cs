using System.ComponentModel.DataAnnotations;

namespace ProductApi.Application.DTOs;

public sealed record ProductResponseDto(
    int Id,
    string Name,
    decimal Price,
    int Quantity,
    string RowVersion
);

public sealed record CreateProductDto(
    [MinLength(1, ErrorMessage = "El nombre no puede estar vacío."),
     MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    string Name,

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que 0")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
    int Quantity
);

public sealed record UpdateProductDto(
    [MinLength(1, ErrorMessage = "El nombre no puede estar vacío."),
     MaxLength(100, ErrorMessage = "El nombre no puede superar 100 caracteres.")]
    string Name,

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor que 0")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "La cantidad no puede ser negativa")]
    int Quantity,

    [Required(ErrorMessage = "El RowVersion es obligatorio.")]
    string RowVersion
);

public sealed record PatchProductDto(
    string? Name,
    decimal? Price,
    int? Quantity,

    [Required(ErrorMessage = "El RowVersion es obligatorio.")]
    string RowVersion
);

public sealed record PagedResultDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);