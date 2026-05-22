using Microsoft.Extensions.Logging;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Infrastructure.Repositories;

// Patrón Repository: encapsula toda la lógica de acceso a datos detrás
// de una interfaz. El dominio y la aplicación no saben si los datos
// viven en memoria, SQL Server, PostgreSQL u otro origen.
//
// SOLID - LSP (Liskov Substitution Principle):
// InMemoryProductRepository puede sustituirse por EfProductRepository
// en cualquier contexto sin que el comportamiento observable cambie.
// Ambas implementan IProductRepository con el mismo contrato.
public sealed class InMemoryProductRepository : IProductRepository
{
    private static readonly List<Product> _store = [];
    private static int _nextId = 1;
    private static readonly Lock _lock = new();

    private readonly ILogger<InMemoryProductRepository> _logger;

    public InMemoryProductRepository(ILogger<InMemoryProductRepository> logger)
    {
        _logger = logger;
    }

    public Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? nameFilter,
        string? sortBy, bool sortDescending,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Repository.GetAll");
        lock (_lock)
        {
            var query = _store.AsQueryable();

            // Filtro
            if (!string.IsNullOrWhiteSpace(nameFilter))
                query = query.Where(p =>
                    p.Name.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));

            // Ordenación
            query = sortBy?.ToLowerInvariant() switch
            {
                "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => sortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                "quantity" => sortDescending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
                _ => query.OrderBy(p => p.Id)
            };

            var total = query.Count();

            // Paginación
            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<(IEnumerable<Product>, int)>((items, total));
        }
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Repository.GetById → id={Id}", id);
        lock (_lock)
        {
            var product = _store.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(product);
        }
    }

    public Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("Repository.Create → name={Name}", product.Name);
        lock (_lock)
        {
            product.Id = _nextId++;
            product.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            _store.Add(product);
            return Task.FromResult(product);
        }
    }

    public Task<Product?> UpdateAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("Repository.Update → id={Id}", product.Id);
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(p => p.Id == product.Id);
            if (existing is null) return Task.FromResult<Product?>(null);

            // Control de concurrencia optimista
            if (!existing.RowVersion.SequenceEqual(product.RowVersion))
                throw new ConcurrencyException();

            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.Quantity = product.Quantity;
            existing.RowVersion = BitConverter.GetBytes(DateTime.UtcNow.Ticks);

            return Task.FromResult<Product?>(existing);
        }
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("Repository.Delete → id={Id}", id);
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(p => p.Id == id);
            if (existing is null) return Task.FromResult(false);
            _store.Remove(existing);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Any(p => p.Id == id));
        }
    }
}