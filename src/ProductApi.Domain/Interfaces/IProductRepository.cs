using ProductApi.Domain.Entities;

namespace ProductApi.Domain.Interfaces;

/// <summary>
/// Contrato del repositorio de productos.
/// Principio D de SOLID: la Application Layer depende de esta abstracción,
/// no de la implementación concreta (InMemory, EF Core, etc.).
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
