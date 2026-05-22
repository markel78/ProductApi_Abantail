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
    // ── Fixtures ────────────────────────────────────────────────────────────
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _sut; // System Under Test

    public ProductServiceTests()
    {
        _repoMock = new Mock<IProductRepository>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _sut = new ProductService(_repoMock.Object, _loggerMock.Object);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────
    private static Product MakeProduct(int id = 1, string name = "Widget", decimal price = 9.99m, int qty = 10)
        => new() { Id = id, Name = name, Price = price, Quantity = qty, RowVersion = BitConverter.GetBytes(1L) };

    private static string RowVersionBase64(long ticks = 1L)
        => Convert.ToBase64String(BitConverter.GetBytes(ticks));

    // ════════════════════════════════════════════════════════════════════════
    // GetAllAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult_WithCorrectTotalPages()
    {
        // Arrange
        var products = Enumerable.Range(1, 3).Select(i => MakeProduct(i, $"P{i}")).ToList();
        _repoMock
            .Setup(r => r.GetAllAsync(1, 2, null, null, false, default))
            .ReturnsAsync((products.Take(2), 3));

        // Act
        var result = await _sut.GetAllAsync(1, 2, null, null, false);

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(2, result.TotalPages);   // ceil(3/2) = 2
        Assert.Equal(1, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsTotalCountZero()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetAllAsync(1, 10, null, null, false, default))
            .ReturnsAsync((Enumerable.Empty<Product>(), 0));

        // Act
        var result = await _sut.GetAllAsync(1, 10, null, null, false);

        // Assert
        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_MapsAllDtoFields_Correctly()
    {
        // Arrange
        var product = MakeProduct(7, "Gadget", 19.99m, 5);
        _repoMock
            .Setup(r => r.GetAllAsync(1, 10, null, null, false, default))
            .ReturnsAsync((new[] { product }, 1));

        // Act
        var result = await _sut.GetAllAsync(1, 10, null, null, false);
        var dto = result.Items.Single();

        // Assert
        Assert.Equal(7, dto.Id);
        Assert.Equal("Gadget", dto.Name);
        Assert.Equal(19.99m, dto.Price);
        Assert.Equal(5, dto.Quantity);
        Assert.Equal(Convert.ToBase64String(product.RowVersion), dto.RowVersion);
    }

    // ════════════════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var product = MakeProduct(1);
        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(product);

        // Act
        var dto = await _sut.GetByIdAsync(1);

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("Widget", dto.Name);
        Assert.Equal(9.99m, dto.Price);
        Assert.Equal(10, dto.Quantity);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.GetByIdAsync(99));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task GetByIdAsync_InvalidId_ThrowsNotFoundException(int id)
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(id));
    }

    // ════════════════════════════════════════════════════════════════════════
    // CreateAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedDto()
    {
        // Arrange
        var dto = new CreateProductDto("Widget", 9.99m, 10);
        var created = MakeProduct(1);

        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(created);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        Assert.Equal(1, result.Id);
        Assert.Equal("Widget", result.Name);
        Assert.Equal(9.99m, result.Price);
        Assert.Equal(10, result.Quantity);
    }

    [Fact]
    public async Task CreateAsync_CallsRepository_ExactlyOnce()
    {
        // Arrange
        var dto = new CreateProductDto("Widget", 9.99m, 10);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .ReturnsAsync(MakeProduct());

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<Product>(), default), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_MapsInputFields_ToEntity()
    {
        // Arrange
        var dto = new CreateProductDto("Nuevo", 5.50m, 3);
        Product? captured = null;

        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "Nuevo", 5.50m, 3));

        // Act
        await _sut.CreateAsync(dto);

        // Assert — el servicio pasa los valores correctos al repositorio
        Assert.NotNull(captured);
        Assert.Equal("Nuevo", captured.Name);
        Assert.Equal(5.50m, captured.Price);
        Assert.Equal(3, captured.Quantity);
    }

    // ════════════════════════════════════════════════════════════════════════
    // UpdateAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_ExistingProduct_ReturnsUpdatedDto()
    {
        // Arrange
        var existing = MakeProduct(1);
        var dto = new UpdateProductDto("Updated", 15m, 20, RowVersionBase64());
        var updated = MakeProduct(1, "Updated", 15m, 20);

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default)).ReturnsAsync(updated);

        // Act
        var result = await _sut.UpdateAsync(1, dto);

        // Assert
        Assert.Equal("Updated", result.Name);
        Assert.Equal(15m, result.Price);
        Assert.Equal(20, result.Quantity);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);
        var dto = new UpdateProductDto("X", 1m, 1, RowVersionBase64());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(99, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReturnsNull_ThrowsNotFoundException()
    {
        // Simula concurrencia: el producto desaparece entre GetById y Update
        var existing = MakeProduct(1);
        var dto = new UpdateProductDto("X", 1m, 1, RowVersionBase64());

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
                 .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.UpdateAsync(1, dto));
    }

    // ════════════════════════════════════════════════════════════════════════
    // PatchAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PatchAsync_OnlyName_UpdatesNameOnly()
    {
        // Arrange
        var existing = MakeProduct(1, "Old", 9.99m, 10);
        var dto = new PatchProductDto("New", null, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "New", 9.99m, 10));

        // Act
        await _sut.PatchAsync(1, dto);

        // Assert — precio y cantidad no cambian
        Assert.NotNull(captured);
        Assert.Equal("New", captured.Name);
        Assert.Equal(9.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    [Fact]
    public async Task PatchAsync_OnlyPrice_UpdatesPriceOnly()
    {
        // Arrange
        var existing = MakeProduct(1, "Widget", 9.99m, 10);
        var dto = new PatchProductDto(null, 49.99m, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(MakeProduct(1, "Widget", 49.99m, 10));

        // Act
        await _sut.PatchAsync(1, dto);

        // Assert — nombre y cantidad no cambian
        Assert.NotNull(captured);
        Assert.Equal("Widget", captured.Name);
        Assert.Equal(49.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    [Fact]
    public async Task PatchAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((Product?)null);
        var dto = new PatchProductDto(null, null, null, RowVersionBase64());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.PatchAsync(99, dto));
    }

    [Fact]
    public async Task PatchAsync_AllFieldsNull_LeavesEntityUnchanged()
    {
        // Arrange
        var existing = MakeProduct(1, "Widget", 9.99m, 10);
        var dto = new PatchProductDto(null, null, null, RowVersionBase64());
        Product? captured = null;

        _repoMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), default))
            .Callback<Product, CancellationToken>((p, _) => captured = p)
            .ReturnsAsync(existing);

        // Act
        await _sut.PatchAsync(1, dto);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("Widget", captured.Name);
        Assert.Equal(9.99m, captured.Price);
        Assert.Equal(10, captured.Quantity);
    }

    // ════════════════════════════════════════════════════════════════════════
    // DeleteAsync
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DeleteAsync_ExistingId_CallsRepositoryOnce()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(true);

        // Act
        await _sut.DeleteAsync(1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.DeleteAsync(99, default)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.DeleteAsync(99));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotCallGetById_BeforeDelete()
    {
        // El servicio debe delegar directamente en DeleteAsync del repo,
        // sin hacer un GetById previo innecesario
        _repoMock.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(true);

        await _sut.DeleteAsync(1);

        _repoMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), default), Times.Never);
    }
}