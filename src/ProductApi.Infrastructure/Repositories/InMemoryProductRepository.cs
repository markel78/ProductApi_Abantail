using Microsoft.Extensions.Logging;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Infrastructure.Repositories;

/// <summary>
/// Implementación en memoria del repositorio de productos.
/// Usa una lista estática con bloqueo para simular acceso concurrente.
///
/// Patrón Repository: encapsula toda la lógica de acceso a datos.
/// Principio O de SOLID: para cambiar a EF Core basta con crear
/// EfProductRepository : IProductRepository sin tocar ninguna otra capa.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    // Estado estático compartido entre todas las instancias (simula una BD)
    private static readonly List<Product> _store = new()
    {
        new Product { Id = 1, Name = "Laptop Pro",   Price = 1299.99m, Quantity = 15, RowVersion = 1 },
        new Product { Id = 2, Name = "Wireless Mouse", Price = 29.99m, Quantity = 100, RowVersion = 1 },
        new Product { Id = 3, Name = "USB-C Hub",    Price = 49.99m,  Quantity = 50, RowVersion = 1 }
    };
    private static int _nextId = 4;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private readonly ILogger<InMemoryProductRepository> _logger;

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Repository: GetAll");
        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Devolvemos copias para evitar mutación externa
            return _store.Select(Clone).ToList();
        }
        finally { _lock.Release(); }
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Repository: GetById {Id}", id);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var product = _store.FirstOrDefault(p => p.Id == id);
            return product is null ? null : Clone(product);
        }
        finally { _lock.Release(); }
    }

    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Repository: Add product '{Name}'", product.Name);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            product.Id = _nextId++;
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.RowVersion = 1;
            _store.Add(product);
            return Clone(product);
        }
        finally { _lock.Release(); }
    }

    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Repository: Update product {Id}", product.Id);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var index = _store.FindIndex(p => p.Id == product.Id);
            if (index < 0) throw new InvalidOperationException($"Product {product.Id} not found in store.");
            _store[index] = product;
            return Clone(product);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Repository: Delete product {Id}", id);
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var product = _store.FirstOrDefault(p => p.Id == id);
            if (product is not null) _store.Remove(product);
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { return _store.Any(p => p.Id == id); }
        finally { _lock.Release(); }
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try { return _store.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)); }
        finally { _lock.Release(); }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static Product Clone(Product p) => new()
    {
        Id         = p.Id,
        Name       = p.Name,
        Price      = p.Price,
        Quantity   = p.Quantity,
        RowVersion = p.RowVersion,
        CreatedAt  = p.CreatedAt,
        UpdatedAt  = p.UpdatedAt
    };
}
