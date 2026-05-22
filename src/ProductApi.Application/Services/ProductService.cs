using Microsoft.Extensions.Logging;
using ProductApi.Application.DTOs;
using ProductApi.Application.Interfaces;
using ProductApi.Application.Mappings;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Application.Services;

/// <summary>
/// Servicio que implementa los use-cases de producto.
/// Principio S (Single Responsibility): sólo contiene lógica de negocio.
/// Principio D (Dependency Inversion): depende de IProductRepository, no de la implementación.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository repository, ILogger<ProductService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET ALL (paginación + filtro + ordenación)
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<PagedResult<ProductDto>> GetAllAsync(
        ProductQueryParams query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting products. Page={Page}, PageSize={PageSize}, Filter={Filter}, SortBy={SortBy} {SortOrder}",
            query.Page, query.PageSize, query.NameFilter, query.SortBy, query.SortOrder);

        try
        {
            var all = await _repository.GetAllAsync(cancellationToken);

            // Filtrado
            if (!string.IsNullOrWhiteSpace(query.NameFilter))
                all = all.Where(p => p.Name.Contains(query.NameFilter, StringComparison.OrdinalIgnoreCase));

            if (query.MinPrice.HasValue)
                all = all.Where(p => p.Price >= query.MinPrice.Value);

            if (query.MaxPrice.HasValue)
                all = all.Where(p => p.Price <= query.MaxPrice.Value);

            // Ordenación
            all = (query.SortBy.ToLowerInvariant(), query.SortOrder.ToLowerInvariant()) switch
            {
                ("name",  "asc")  => all.OrderBy(p => p.Name),
                ("name",  "desc") => all.OrderByDescending(p => p.Name),
                ("price", "asc")  => all.OrderBy(p => p.Price),
                ("price", "desc") => all.OrderByDescending(p => p.Price),
                ("quantity", "asc")  => all.OrderBy(p => p.Quantity),
                ("quantity", "desc") => all.OrderByDescending(p => p.Quantity),
                ("id",    "desc") => all.OrderByDescending(p => p.Id),
                _                 => all.OrderBy(p => p.Id)
            };

            var totalCount = all.Count();

            // Paginación
            var items = all
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(p => p.ToDto())
                .ToList();

            return new PagedResult<ProductDto>(items, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products list.");
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET BY ID
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting product {Id}", id);

        try
        {
            var product = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ProductNotFoundException(id);

            return product.ToDto();
        }
        catch (ProductNotFoundException)
        {
            _logger.LogWarning("Product {Id} not found.", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {Id}.", id);
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // CREATE
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating product with name '{Name}'", dto.Name);

        try
        {
            if (await _repository.ExistsByNameAsync(dto.Name, cancellationToken))
                throw new DuplicateProductException(dto.Name);

            var entity = dto.ToEntity();
            var created = await _repository.AddAsync(entity, cancellationToken);

            _logger.LogInformation("Product created with id {Id}", created.Id);
            return created.ToDto();
        }
        catch (DuplicateProductException)
        {
            _logger.LogWarning("Duplicate product name '{Name}'.", dto.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product '{Name}'.", dto.Name);
            throw;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // UPDATE (PUT — reemplazo completo)
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ProductDto> UpdateAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating product {Id}", id);

        try
        {
            var existing = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ProductNotFoundException(id);

            // Control de concurrencia optimista
            if (existing.RowVersion != dto.RowVersion)
                throw new ProductConcurrencyException(id);

            existing.Name = dto.Name;
            existing.Price = dto.Price;
            existing.Quantity = dto.Quantity;
            existing.RowVersion++;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("Product {Id} updated.", id);
            return updated.ToDto();
        }
        catch (ProductNotFoundException) { _logger.LogWarning("Product {Id} not found for update.", id); throw; }
        catch (ProductConcurrencyException) { _logger.LogWarning("Concurrency conflict on product {Id}.", id); throw; }
        catch (Exception ex) { _logger.LogError(ex, "Error updating product {Id}.", id); throw; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PATCH (actualización parcial)
    // ──────────────────────────────────────────────────────────────────────────
    public async Task<ProductDto> PatchAsync(int id, PatchProductDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Patching product {Id}", id);

        try
        {
            var existing = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ProductNotFoundException(id);

            // Control de concurrencia optimista
            if (existing.RowVersion != dto.RowVersion)
                throw new ProductConcurrencyException(id);

            // Solo se actualizan los campos que vienen informados
            if (dto.Name is not null)     existing.Name     = dto.Name;
            if (dto.Price.HasValue)       existing.Price    = dto.Price.Value;
            if (dto.Quantity.HasValue)    existing.Quantity = dto.Quantity.Value;

            existing.RowVersion++;
            existing.UpdatedAt = DateTime.UtcNow;

            var patched = await _repository.UpdateAsync(existing, cancellationToken);
            _logger.LogInformation("Product {Id} patched.", id);
            return patched.ToDto();
        }
        catch (ProductNotFoundException) { _logger.LogWarning("Product {Id} not found for patch.", id); throw; }
        catch (ProductConcurrencyException) { _logger.LogWarning("Concurrency conflict on patch for product {Id}.", id); throw; }
        catch (Exception ex) { _logger.LogError(ex, "Error patching product {Id}.", id); throw; }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE
    // ──────────────────────────────────────────────────────────────────────────
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product {Id}", id);

        try
        {
            if (!await _repository.ExistsAsync(id, cancellationToken))
                throw new ProductNotFoundException(id);

            await _repository.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Product {Id} deleted.", id);
        }
        catch (ProductNotFoundException) { _logger.LogWarning("Product {Id} not found for deletion.", id); throw; }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting product {Id}.", id); throw; }
    }
}
