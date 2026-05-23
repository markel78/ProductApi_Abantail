using Microsoft.Extensions.Logging;
using Moq;
using ProductApi.Application.DTOs;
using ProductApi.Application.Services;
using ProductApi.Domain.Entities;
using ProductApi.Domain.Exceptions;
using ProductApi.Domain.Interfaces;

namespace ProductApi.Tests;

public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _sut; 

    public ProductServiceTests()
    {
        _repoMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _sut = new ProductService(_repoMock.Object, _loggerMock.Object);
    }

    private static Product MakeProduct(int id = 1, string name = "Widget", decimal price = 9.99m, int qty = 10)
        => new() { Id = id, Name = name, Price = price, Quantity = qty, RowVersion = BitConverter.GetBytes(1L) };

    private static string RowVersionBase64(long ticks = 1L)
        => Convert.ToBase64String(BitConverter.GetBytes(ticks));

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult_WithCorrectTotalPages()
    {
        var products = Enumerable.Range(1, 3).Select(i => MakeProduct(i, $"P{i}")).ToList();
        _repoMock
            .Setup(r => r.GetAllAsync(1, 2, null, null, false, default))
            .ReturnsAsync((products.Take(2), 3));

        var result = await _sut.GetAllAsync(1, 2, null, null, false);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages); 
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsTotalCountZero()
    {
        _repoMock
            .Setup(r => r.GetAllAsync(1, 10, null, null, false, default))
            .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        var result = await _sut.GetAllAsync(1, 10, null, null, false);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllDtoFields_Correctly()
    {
        var product = MakeProduct(7, "Gadget", 19.99m, 5);
        _repoMock
            .Setup(r => r.GetAllAsync(1, 10, null, null, false, default))
            .ReturnsAsync((new[] { product }, 1));

        var result = await _sut.GetAllAsync(1, 10, null, null, false);
        var dto = result.Items.Single();

        Assert.Equal(7, dto.Id);
        Assert.Equal("Gadget", dto.Name);
        Assert.Equal(19.99m, dto.Price);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(Convert.ToBase64String(product.RowVersion), dto.RowVersion);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var product = MakeProduct(1);
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(product);

        var dto = await _sut.GetByIdAsync(1);

        Assert.Equal(1, dto.Id);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal(9.99m, dto.Price);
        Assert.Equal(10, dto.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetByIdAsync(99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetByIdAsync_InvalidId_ThrowsNotFoundException(int id)
    {
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(id));
    }

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        var dto = new CreateProductDto("Widget", 9.99m, 10);
        var created = MakeProduct(1);

        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(created);

        var result = await _sut.CreateAsync(dto);

        Assert.Equal(1, result.Id);
        Assert.Equal("Widget", result.Name);
        Assert.Equal(9.99m, result.Price);
        Assert.Equal(10, result.Quantity);
    }

    [Fact]
    public async Task CreateAsync_CallsRepository_ExactlyOnce()
    {
        var dto = new CreateProductDto("Widget", 9.99m, 10);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(MakeProduct());

        await _sut.CreateAsync(dto);

        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MapsInputFields_ToEntity()
    {
        var dto = new CreateProductDto("Nuevo", 5.50m, 3);
        Product? captured = null;

        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "Nuevo", 5.50m, 3));

        await _sut.CreateAsync(dto);

        Assert.NotNull(captured);
        Assert.Equal("Nuevo", captured.Name);
        Assert.Equal(5.50m, captured.Price);
        Assert.Equal(3, captured.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_ExistingProduct_ReturnsUpdatedDto()
    {
        var existing = MakeProduct(1);
        var dto = new UpdateProductDto("Updated", 15m, 20, RowVersionBase64());
        var updated = MakeProduct(1, "Updated", 15m, 20);

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync(updated);

        var result = await _sut.UpdateAsync(1, dto);

        Assert.Equal("Updated", result.Name);
        Assert.Equal(15m, result.Price);
        Assert.Equal(20, result.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);
        var dto = new UpdateProductDto("X", 1m, 1, RowVersionBase64());

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsNull_ThrowsNotFoundException()
    {
        var existing = MakeProduct(1);
        var dto = new UpdateProductDto("X", 1m, 1, RowVersionBase64());

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
                 .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, dto));
    }

    [Fact]
    public async Task PatchAsync_OnlyName_UpdatesNameOnly()
    {
        var existing = MakeProduct(1, "Old", 9.99m, 10);
        var dto = new PatchProductDto("New", null, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "New", 9.99m, 10));

        await _sut.PatchAsync(1, dto);

        Assert.NotNull(captured);
        Assert.Equal("New", captured.Name);
        Assert.Equal(9.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    [Fact]
    public async Task PatchAsync_OnlyPrice_UpdatesPriceOnly()
    {
        var existing = MakeProduct(1, "Widget", 9.99m, 10);
        var dto = new PatchProductDto(null, 49.99m, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "Widget", 49.99m, 10));

        await _sut.PatchAsync(1, dto);

        Assert.NotNull(captured);
        Assert.Equal("Widget", captured.Name);
        Assert.Equal(49.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    [Fact]
    public async Task PatchAsync_NonExistingId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);
        var dto = new PatchProductDto(null, null, null, RowVersionBase64());

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.PatchAsync(99, dto));
    }

    [Fact]
    public async Task PatchAsync_AllFieldsNull_LeavesEntityUnchanged()
    {
        var existing = MakeProduct(1, "Widget", 9.99m, 10);
        var dto = new PatchProductDto(null, null, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(existing);

        await _sut.PatchAsync(1, dto);

        Assert.NotNull(captured);
        Assert.Equal("Widget", captured.Name);
        Assert.Equal(9.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_CallsRepositoryOnce()
    {
        _repoMock.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(true);
        
        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ThrowsNotFoundException()
    {
        _repoMock.Setup(r => r.DeleteAsync(99, default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotCallGetById_BeforeDelete()
    {
        _repoMock.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(true);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), default), Times.Never);
    }
}