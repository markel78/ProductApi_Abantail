using ProductApi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Domain.Interfaces
{
    // SOLID - DIP (Dependency Inversion Principle):
    // Las capas superiores (Application) dependen de esta abstracción,
    // nunca de la implementación concreta (InMemoryProductRepository).
    //
    // SOLID - ISP (Interface Segregation Principle):
    // La interfaz expone solo las operaciones que los consumidores necesitan
    public interface IProductRepository
    {
        Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(
       int page, int pageSize,
       string? nameFilter,
       string? sortBy, bool sortDescending,
       CancellationToken ct = default);

        Task<Product?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Product> CreateAsync(Product product, CancellationToken ct = default);
        Task<Product?> UpdateAsync(Product product, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id, CancellationToken ct = default);
        Task<bool> ExistsAsync(int id, CancellationToken ct = default);
    }
}
