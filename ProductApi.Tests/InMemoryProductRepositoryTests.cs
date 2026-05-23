using Microsoft.Extensions.Logging;
using Moq;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Infrastructure.Repositories;

namespace ProductApi.Tests;
public class InMemoryProductRepositoryTests : IAsyncLifetime
{
    private readonly InMemoryProductRepository _sut;

    public InMemoryProductRepositoryTests()
    {
        var logger = new Mock<ILogger<InMemoryProductRepository>>().Object;
        _sut = new InMemoryProductRepository(logger);
    }

    // Limpia el store estático antes y después de cada test
    public Task InitializeAsync() => ResetStoreAsync();
    public Task DisposeAsync() => ResetStoreAsync();

    private static Task ResetStoreAsync()
    {
        var storeField = typeof(InMemoryProductRepository)
            .GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var nextIdField = typeof(InMemoryProductRepository)
            .GetField("_nextId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        ((System.Collections.Generic.List<Product>)storeField.GetValue(null)!).Clear();
        nextIdField.SetValue(null, 1);

        return Task.CompletedTask;
    }

    private Task<Product> SeedAsync(string name = "Widget", decimal price = 9.99m, int qty = 5)
        => _sut.CreateAsync(new Product { Name = name, Price = price, Quantity = qty });

    [Fact]
    public async Task CreateAsync_AssignsIncrementalId()
    {
        var p1 = await SeedAsync("A");
        var p2 = await SeedAsync("B");

        Assert.Equal(1, p1.Id);
        Assert.Equal(2, p2.Id);
    }

    [Fact]
    public async Task CreateAsync_SetsRowVersion()
    {
        var product = await SeedAsync();

        Assert.NotNull(product.RowVersion);
        Assert.Equal(8, product.RowVersion.Length);
    }

    [Fact]
    public async Task CreateAsync_StoresProductInMemory()
    {
        await SeedAsync("Widget");

        var (items, total) = await _sut.GetAllAsync(1, 10, null, null, false);

        Assert.Equal(1, total);
        Assert.Equal("Widget", items.Single().Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var seeded = await SeedAsync("Widget", 9.99m, 5);

        var result = await _sut.GetByIdAsync(seeded.Id);

        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal("Widget", result.Name);
        Assert.Equal(9.99m, result.Price);
        Assert.Equal(5, result.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTotalCount_Correctly()
    {
        await SeedAsync("A");
        await SeedAsync("B");
        await SeedAsync("C");

        var (_, total) = await _sut.GetAllAsync(1, 10, null, null, false);

        Assert.Equal(3, total);
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        for (var i = 1; i <= 5; i++) await SeedAsync($"P{i}");

        var (items, total) = await _sut.GetAllAsync(2, 2, null, null, false);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count());
    }

    [Fact]
    public async Task GetAllAsync_LastPage_ReturnsRemainingItems()
    {
        for (var i = 1; i <= 5; i++) await SeedAsync($"P{i}");

        var (items, _) = await _sut.GetAllAsync(3, 2, null, null, false);

        Assert.Single(items);
    }

    [Fact]
    public async Task GetAllAsync_NameFilter_ReturnMatchingProducts()
    {
        await SeedAsync("Widget Pro");
        await SeedAsync("Gadget");
        await SeedAsync("Widget Lite");

        var (items, total) = await _sut.GetAllAsync(1, 10, "widget", null, false);

        Assert.Equal(2, total);
        Assert.All(items, p => Assert.Contains("Widget", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAllAsync_NameFilter_IsCaseInsensitive()
    {
        await SeedAsync("Widget");

        var (items, total) = await _sut.GetAllAsync(1, 10, "WIDGET", null, false);

        Assert.Equal(1, total);
        Assert.Equal("Widget", items.Single().Name);
    }

    [Fact]
    public async Task GetAllAsync_NameFilter_NoMatch_ReturnsEmpty()
    {
        await SeedAsync("Widget");

        var (items, total) = await _sut.GetAllAsync(1, 10, "xyz", null, false);

        Assert.Equal(0, total);
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetAllAsync_SortByName_Ascending_ReturnsSorted()
    {
        await SeedAsync("Zebra");
        await SeedAsync("Apple");
        await SeedAsync("Mango");

        var (items, _) = await _sut.GetAllAsync(1, 10, null, "name", false);
        var names = items.Select(p => p.Name).ToList();

        Assert.Equal(["Apple", "Mango", "Zebra"], names);
    }

    [Fact]
    public async Task GetAllAsync_SortByPrice_Descending_ReturnsSorted()
    {
        await SeedAsync("A", price: 5m);
        await SeedAsync("B", price: 20m);
        await SeedAsync("C", price: 10m);

        var (items, _) = await _sut.GetAllAsync(1, 10, null, "price", sortDescending: true);
        var prices = items.Select(p => p.Price).ToList();

        Assert.Equal([20m, 10m, 5m], prices);
    }

    [Fact]
    public async Task GetAllAsync_DefaultSort_OrdersById()
    {
        await SeedAsync("C");
        await SeedAsync("A");
        await SeedAsync("B");

        var (items, _) = await _sut.GetAllAsync(1, 10, null, null, false);
        var ids = items.Select(p => p.Id).ToList();

        Assert.Equal(ids.OrderBy(x => x).ToList(), ids);
    }

    [Fact]
    public async Task UpdateAsync_ValidRowVersion_UpdatesProduct()
    {
        var seeded = await SeedAsync("Old", 5m, 1);

        seeded.Name = "New";
        seeded.Price = 99m;
        seeded.Quantity = 50;

        var updated = await _sut.UpdateAsync(seeded);

        Assert.NotNull(updated);
        Assert.Equal("New", updated.Name);
        Assert.Equal(99m, updated.Price);
        Assert.Equal(50, updated.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_ValidRowVersion_RefreshesRowVersion()
    {
        var seeded = await SeedAsync();
        var originalVersion = seeded.RowVersion.ToArray();

        await Task.Delay(1); // garantiza tick diferente
        var updated = await _sut.UpdateAsync(seeded);

        Assert.NotNull(updated);
        Assert.False(originalVersion.SequenceEqual(updated.RowVersion));
    }

    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ThrowsConcurrencyException()
    {
        var seeded = await SeedAsync();

        var stale = new Product
        {
            Id = seeded.Id,
            Name = seeded.Name,
            Price = seeded.Price,
            Quantity = seeded.Quantity,
            RowVersion = BitConverter.GetBytes(0L)
        };

        await Assert.ThrowsAsync<ConcurrencyException>(
            () => _sut.UpdateAsync(stale));
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var ghost = new Product { Id = 999, Name = "X", Price = 1m, Quantity = 1, RowVersion = BitConverter.GetBytes(1L) };

        var result = await _sut.UpdateAsync(ghost);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndRemovesProduct()
    {
        var seeded = await SeedAsync();

        var deleted = await _sut.DeleteAsync(seeded.Id);
        var found = await _sut.GetByIdAsync(seeded.Id);

        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotAffectOtherProducts()
    {
        var p1 = await SeedAsync("A");
        var p2 = await SeedAsync("B");

        await _sut.DeleteAsync(p1.Id);

        var remaining = await _sut.GetByIdAsync(p2.Id);
        Assert.NotNull(remaining);
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        var seeded = await SeedAsync();

        var exists = await _sut.ExistsAsync(seeded.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistingId_ReturnsFalse()
    {
        var exists = await _sut.ExistsAsync(999);

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_AfterDelete_ReturnsFalse()
    {
        var seeded = await SeedAsync();
        await _sut.DeleteAsync(seeded.Id);

        var exists = await _sut.ExistsAsync(seeded.Id);

        Assert.False(exists);
    }
}