using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using Xunit;

namespace ProductService.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly ProductDbContext _context;
    private readonly Services.ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ProductDbContext(options);
        _service = new Services.ProductService(_context);
    }

    public void Dispose() => _context.Dispose();

    // 
    // CreateAsync
    // 

    [Fact]
    public async Task CreateAsync_ValidDto_ReturnsCreatedProduct()
    {
        var dto = new CreateProductDto("Laptop", "Desc", "Electrónica", null, 999.99m, 10);

        var result = await _service.CreateAsync(dto);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Laptop", result.Name);
        Assert.Equal("Electrónica", result.Category);
        Assert.Equal(999.99m, result.Price);
        Assert.Equal(10, result.Stock);
    }

    [Fact]
    public async Task CreateAsync_PersistsToDatabase()
    {
        var dto = new CreateProductDto("Monitor", null, "Periféricos", null, 299m, 5);

        await _service.CreateAsync(dto);

        Assert.Equal(1, await _context.Products.CountAsync());
    }

    // 
    // GetByIdAsync
    // 

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var product = new Product { Name = "Mouse", Category = "Periféricos", Price = 49.99m, Stock = 20 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(product.Id);

        Assert.NotNull(result);
        Assert.Equal("Mouse", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // 
    // GetAllPagedAsync - filtros y paginación
    // 

    [Fact]
    public async Task GetAllPagedAsync_NoFilter_ReturnsAllProducts()
    {
        _context.Products.AddRange(
            new Product { Name = "A", Category = "Cat1", Price = 10m, Stock = 5 },
            new Product { Name = "B", Category = "Cat2", Price = 20m, Stock = 10 }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllPagedAsync(new ProductFilterDto { Page = 1, PageSize = 10 });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Data.Count());
    }

    [Fact]
    public async Task GetAllPagedAsync_FilterByName_ReturnsMatchingProducts()
    {
        _context.Products.AddRange(
            new Product { Name = "Laptop HP", Category = "Electrónica", Price = 1000m, Stock = 5 },
            new Product { Name = "Mouse", Category = "Periféricos", Price = 50m, Stock = 10 }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllPagedAsync(new ProductFilterDto { Name = "laptop", Page = 1, PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Laptop HP", result.Data.First().Name);
    }

    [Fact]
    public async Task GetAllPagedAsync_FilterByCategory_ReturnsMatchingProducts()
    {
        _context.Products.AddRange(
            new Product { Name = "A", Category = "Electrónica", Price = 10m, Stock = 5 },
            new Product { Name = "B", Category = "Periféricos", Price = 20m, Stock = 10 }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllPagedAsync(new ProductFilterDto { Category = "electrónica", Page = 1, PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Electrónica", result.Data.First().Category);
    }

    [Fact]
    public async Task GetAllPagedAsync_FilterByMinPrice_ReturnsCorrectProducts()
    {
        _context.Products.AddRange(
            new Product { Name = "Cheap", Category = "X", Price = 10m, Stock = 5 },
            new Product { Name = "Expensive", Category = "X", Price = 500m, Stock = 3 }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetAllPagedAsync(new ProductFilterDto { MinPrice = 100m, Page = 1, PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Expensive", result.Data.First().Name);
    }

    [Fact]
    public async Task GetAllPagedAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 15; i++)
            _context.Products.Add(new Product { Name = $"P{i:D2}", Category = "X", Price = i * 10m, Stock = i });
        await _context.SaveChangesAsync();

        var result = await _service.GetAllPagedAsync(new ProductFilterDto { Page = 2, PageSize = 5 });

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(5, result.Data.Count());
        Assert.Equal(2, result.Page);
    }

    // 
    // UpdateAsync
    // 

    [Fact]
    public async Task UpdateAsync_ExistingProduct_UpdatesAllFields()
    {
        var product = new Product { Name = "Old Name", Category = "Old Cat", Price = 10m, Stock = 5 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var dto = new UpdateProductDto("New Name", "New Desc", "New Cat", null, 99m, 50);
        var result = await _service.UpdateAsync(product.Id, dto);

        Assert.NotNull(result);
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Cat", result.Category);
        Assert.Equal(99m, result.Price);
        Assert.Equal(50, result.Stock);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var dto = new UpdateProductDto("X", null, "Y", null, 1m, 1);
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto);
        Assert.Null(result);
    }

    // 
    // DeleteAsync
    // 

    [Fact]
    public async Task DeleteAsync_ExistingProduct_ReturnsTrueAndRemovesFromDb()
    {
        var product = new Product { Name = "Del", Category = "X", Price = 1m, Stock = 1 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteAsync(product.Id);

        Assert.True(result);
        Assert.Equal(0, await _context.Products.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        var result = await _service.DeleteAsync(Guid.NewGuid());
        Assert.False(result);
    }

    // 
    // UpdateStockAsync
    // 

    [Fact]
    public async Task UpdateStockAsync_PositiveAdjustment_IncreasesStock()
    {
        var product = new Product { Name = "P", Category = "X", Price = 10m, Stock = 10 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateStockAsync(product.Id, new StockUpdateRequestDto(5, "Purchase"));

        Assert.NotNull(result);
        Assert.Equal(15, result.NewStock);
        Assert.Equal(15, (await _context.Products.FindAsync(product.Id))!.Stock);
    }

    [Fact]
    public async Task UpdateStockAsync_NegativeAdjustment_DecreasesStock()
    {
        var product = new Product { Name = "P", Category = "X", Price = 10m, Stock = 10 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateStockAsync(product.Id, new StockUpdateRequestDto(-3, "Sale"));

        Assert.NotNull(result);
        Assert.Equal(7, result.NewStock);
    }

    [Fact]
    public async Task UpdateStockAsync_InsufficientStock_ThrowsInvalidOperationException()
    {
        var product = new Product { Name = "P", Category = "X", Price = 10m, Stock = 2 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateStockAsync(product.Id, new StockUpdateRequestDto(-5, "Sale")));
    }

    [Fact]
    public async Task UpdateStockAsync_ExactStock_DecreasesToZero()
    {
        var product = new Product { Name = "P", Category = "X", Price = 10m, Stock = 5 };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var result = await _service.UpdateStockAsync(product.Id, new StockUpdateRequestDto(-5, "Sale"));

        Assert.Equal(0, result!.NewStock);
    }

    [Fact]
    public async Task UpdateStockAsync_NonExistingProduct_ReturnsNull()
    {
        var result = await _service.UpdateStockAsync(Guid.NewGuid(), new StockUpdateRequestDto(1, "Purchase"));
        Assert.Null(result);
    }
}
