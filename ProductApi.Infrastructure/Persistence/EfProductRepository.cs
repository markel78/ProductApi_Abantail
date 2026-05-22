using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Infrastructure.Persistence;

// Para activar: cambiar en Program.cs
// AddScoped<IProductRepository, InMemoryProductRepository>()
// por
// AddScoped<IProductRepository, EfProductRepository>()
public sealed class EfProductRepository : IProductRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EfProductRepository> _logger;

    public EfProductRepository(AppDbContext db, ILogger<EfProductRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, string? nameFilter,
        string? sortBy, bool sortDescending,
        CancellationToken ct = default)
    {
        _logger.LogInformation("EfRepository.GetAll");

        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(p => p.Name.Contains(nameFilter));

        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => sortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "quantity" => sortDescending ? query.OrderByDescending(p => p.Quantity) : query.OrderBy(p => p.Quantity),
            _ => query.OrderBy(p => p.Id)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("EfRepository.GetById → id={Id}", id);
        return await _db.Products.FindAsync([id], ct);
    }

    public async Task<Product> CreateAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("EfRepository.Create → name={Name}", product.Name);
        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task<Product?> UpdateAsync(Product product, CancellationToken ct = default)
    {
        _logger.LogInformation("EfRepository.Update → id={Id}", product.Id);
        var existing = await _db.Products.FindAsync([product.Id], ct);
        if (existing is null) return null;

        existing.Name = product.Name;
        existing.Price = product.Price;
        existing.Quantity = product.Quantity;

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException();
        }

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        _logger.LogInformation("EfRepository.Delete → id={Id}", id);
        var existing = await _db.Products.FindAsync([id], ct);
        if (existing is null) return false;

        _db.Products.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken ct = default)
    {
        return await _db.Products.AnyAsync(p => p.Id == id, ct);
    }
}