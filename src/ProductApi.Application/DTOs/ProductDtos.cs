using System.ComponentModel.DataAnnotations;

namespace ProductApi.Application.DTOs;

// ──────────────────────────────────────────────────────────────────────────────
// Response DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>DTO de salida para un producto.</summary>
public record ProductDto(
    int Id,
    string Name,
    decimal Price,
    int Quantity,
    uint RowVersion,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

/// <summary>Respuesta paginada genérica.</summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// ──────────────────────────────────────────────────────────────────────────────
// Request DTOs
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>DTO para crear un producto.</summary>
public record CreateProductDto(
    [Required, StringLength(200, MinimumLength = 1)]
    string Name,

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    decimal Price,

    [Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative.")]
    int Quantity
);

/// <summary>DTO para reemplazar un producto completo (PUT).</summary>
public record UpdateProductDto(
    [Required, StringLength(200, MinimumLength = 1)]
    string Name,

    [Range(0.01, double.MaxValue)]
    decimal Price,

    [Range(0, int.MaxValue)]
    int Quantity,

    /// <summary>RowVersion actual para control de concurrencia optimista.</summary>
    [Required]
    uint RowVersion
);

/// <summary>DTO para actualización parcial (PATCH). Todos los campos son opcionales.</summary>
public record PatchProductDto(
    [StringLength(200, MinimumLength = 1)]
    string? Name,

    [Range(0.01, double.MaxValue)]
    decimal? Price,

    [Range(0, int.MaxValue)]
    int? Quantity,

    /// <summary>RowVersion actual para control de concurrencia optimista.</summary>
    [Required]
    uint RowVersion
);

// ──────────────────────────────────────────────────────────────────────────────
// Query parameters
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Parámetros de consulta para listado con paginación, filtro y ordenación.</summary>
public record ProductQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? NameFilter { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string SortBy { get; init; } = "id";
    public string SortOrder { get; init; } = "asc";
}
