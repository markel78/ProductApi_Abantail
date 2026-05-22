using Microsoft.EntityFrameworkCore;
using ProductApi.Domain.Entities;

namespace ProductApi.Infrastructure.Persistence;

// Cuando se active EF Core, este DbContext es el único punto
// de configuración del modelo de datos.
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.Price)
                  .HasColumnType("decimal(18,2)");

            entity.Property(p => p.Quantity)
                  .IsRequired();

            // Control de concurrencia optimista — equivalente al RowVersion en memoria
            entity.Property(p => p.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();
        });
    }
}