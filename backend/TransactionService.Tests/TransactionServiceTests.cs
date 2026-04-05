using Microsoft.EntityFrameworkCore;
using Moq;
using TransactionService.Data;
using TransactionService.DTOs;
using TransactionService.Models;
using TransactionService.Services;
using Xunit;

namespace TransactionService.Tests;

public class TransactionServiceTests : IDisposable
{
    private readonly TransactionDbContext _context;
    private readonly Mock<IProductServiceClient> _productClientMock;
    private readonly Services.TransactionService _service;

    public TransactionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TransactionDbContext(options);
        _productClientMock = new Mock<IProductServiceClient>();
        _service = new Services.TransactionService(_context, _productClientMock.Object);
    }

    public void Dispose() => _context.Dispose();

    private static ProductDto MakeProduct(int stock = 20) =>
        new(Guid.NewGuid(), "Laptop", "Elec", 999m, stock);

    // 
    // CreateAsync - Compras (Purchase)
    // 

    [Fact]
    public async Task CreateAsync_Purchase_PersistsTransactionAndCallsStockUpdate()
    {
        var product = MakeProduct(10);
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);
        _productClientMock.Setup(c => c.UpdateStockAsync(product.Id, 5, "Purchase")).ReturnsAsync(true);

        var dto = new CreateTransactionDto("Purchase", product.Id, 5, 199.99m, null, null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(error);
        Assert.NotNull(transaction);
        Assert.Equal("Purchase", transaction.Type);
        Assert.Equal(5, transaction.Quantity);
        Assert.Equal(199.99m * 5, transaction.TotalPrice);
        Assert.Equal(1, await _context.Transactions.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_Purchase_CallsStockWithPositiveAdjustment()
    {
        var product = MakeProduct(5);
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);
        _productClientMock.Setup(c => c.UpdateStockAsync(product.Id, 3, "Purchase")).ReturnsAsync(true);

        var dto = new CreateTransactionDto("Purchase", product.Id, 3, 50m, null, null);
        await _service.CreateAsync(dto);

        _productClientMock.Verify(c => c.UpdateStockAsync(product.Id, 3, "Purchase"), Times.Once);
    }

    // 
    // CreateAsync - Ventas (Sale)
    // 

    [Fact]
    public async Task CreateAsync_Sale_WithSufficientStock_Succeeds()
    {
        var product = MakeProduct(20);
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);
        _productClientMock.Setup(c => c.UpdateStockAsync(product.Id, -5, "Sale")).ReturnsAsync(true);

        var dto = new CreateTransactionDto("Sale", product.Id, 5, 299m, "Venta cliente", null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(error);
        Assert.NotNull(transaction);
        Assert.Equal("Sale", transaction.Type);
        Assert.Equal(5 * 299m, transaction.TotalPrice);
    }

    [Fact]
    public async Task CreateAsync_Sale_WithInsufficientStock_ReturnsError()
    {
        var product = MakeProduct(3); // solo 3 en stock
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);

        var dto = new CreateTransactionDto("Sale", product.Id, 10, 99m, null, null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(transaction);
        Assert.NotNull(error);
        Assert.Contains("insuficiente", error, StringComparison.OrdinalIgnoreCase);
        _productClientMock.Verify(c => c.UpdateStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_Sale_CallsStockWithNegativeAdjustment()
    {
        var product = MakeProduct(10);
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);
        _productClientMock.Setup(c => c.UpdateStockAsync(product.Id, -4, "Sale")).ReturnsAsync(true);

        await _service.CreateAsync(new CreateTransactionDto("Sale", product.Id, 4, 10m, null, null));

        _productClientMock.Verify(c => c.UpdateStockAsync(product.Id, -4, "Sale"), Times.Once);
    }

    // 
    // CreateAsync - errores de producto
    // 

    [Fact]
    public async Task CreateAsync_ProductNotFound_ReturnsError()
    {
        _productClientMock.Setup(c => c.GetProductAsync(It.IsAny<Guid>())).ReturnsAsync((ProductDto?)null);

        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 1, 10m, null, null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(transaction);
        Assert.NotNull(error);
        Assert.Contains("no encontrado", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_ReturnsError()
    {
        var dto = new CreateTransactionDto("InvalidType", Guid.NewGuid(), 1, 10m, null, null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(transaction);
        Assert.NotNull(error);
        Assert.Contains("inválido", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_ProductServiceUnavailable_ReturnsError()
    {
        _productClientMock.Setup(c => c.GetProductAsync(It.IsAny<Guid>()))
            .ThrowsAsync(new InvalidOperationException("Servicio no disponible"));

        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 1, 10m, null, null);
        var (transaction, error) = await _service.CreateAsync(dto);

        Assert.Null(transaction);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task CreateAsync_TotalPrice_IsCalculatedCorrectly()
    {
        var product = MakeProduct(50);
        _productClientMock.Setup(c => c.GetProductAsync(product.Id)).ReturnsAsync(product);
        _productClientMock.Setup(c => c.UpdateStockAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);

        var dto = new CreateTransactionDto("Purchase", product.Id, 7, 49.99m, null, null);
        var (transaction, _) = await _service.CreateAsync(dto);

        Assert.Equal(7 * 49.99m, transaction!.TotalPrice);
    }

    // 
    // GetByIdAsync
    // 

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTransaction()
    {
        var productId = Guid.NewGuid();
        var t = new Transaction { ProductId = productId, Type = TransactionType.Purchase, Quantity = 3, UnitPrice = 10m, TotalPrice = 30m };
        _context.Transactions.Add(t);
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(productId)).ReturnsAsync(MakeProduct());

        var result = await _service.GetByIdAsync(t.Id);

        Assert.NotNull(result);
        Assert.Equal(t.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    // 
    // UpdateAsync - solo modifica Detail y Date
    // 

    [Fact]
    public async Task UpdateAsync_UpdatesDetailAndDate()
    {
        var productId = Guid.NewGuid();
        var t = new Transaction { ProductId = productId, Type = TransactionType.Sale, Quantity = 1, UnitPrice = 10m, TotalPrice = 10m, Detail = "Old" };
        _context.Transactions.Add(t);
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(productId)).ReturnsAsync(MakeProduct());

        var newDate = DateTimeOffset.UtcNow.AddDays(-1);
        var result = await _service.UpdateAsync(t.Id, new UpdateTransactionDto("New detail", newDate));

        Assert.NotNull(result);
        Assert.Equal("New detail", result.Detail);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateTransactionDto("x", null));
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotChangeQuantityOrType()
    {
        var productId = Guid.NewGuid();
        var t = new Transaction { ProductId = productId, Type = TransactionType.Sale, Quantity = 5, UnitPrice = 20m, TotalPrice = 100m };
        _context.Transactions.Add(t);
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(productId)).ReturnsAsync(MakeProduct());

        await _service.UpdateAsync(t.Id, new UpdateTransactionDto("Updated", null));

        var updated = await _context.Transactions.FindAsync(t.Id);
        Assert.Equal(5, updated!.Quantity);
        Assert.Equal(TransactionType.Sale, updated.Type);
        Assert.Equal(100m, updated.TotalPrice);
    }

    // 
    // DeleteAsync - revierte stock
    // 

    [Fact]
    public async Task DeleteAsync_Sale_RevertsStockWithPositiveAdjustment()
    {
        var productId = Guid.NewGuid();
        var t = new Transaction { ProductId = productId, Type = TransactionType.Sale, Quantity = 5, UnitPrice = 10m, TotalPrice = 50m };
        _context.Transactions.Add(t);
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.UpdateStockAsync(productId, 5, "Sale")).ReturnsAsync(true);

        var (success, error) = await _service.DeleteAsync(t.Id);

        Assert.True(success);
        Assert.Null(error);
        _productClientMock.Verify(c => c.UpdateStockAsync(productId, 5, "Sale"), Times.Once);
        Assert.Equal(0, await _context.Transactions.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_Purchase_RevertsStockWithNegativeAdjustment()
    {
        var productId = Guid.NewGuid();
        var t = new Transaction { ProductId = productId, Type = TransactionType.Purchase, Quantity = 10, UnitPrice = 5m, TotalPrice = 50m };
        _context.Transactions.Add(t);
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.UpdateStockAsync(productId, -10, "Purchase")).ReturnsAsync(true);

        var (success, error) = await _service.DeleteAsync(t.Id);

        Assert.True(success);
        _productClientMock.Verify(c => c.UpdateStockAsync(productId, -10, "Purchase"), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalseWithNoError()
    {
        var (success, error) = await _service.DeleteAsync(Guid.NewGuid());

        Assert.False(success);
        Assert.Null(error);
    }

    // 
    // GetAllPagedAsync - filtros
    // 

    [Fact]
    public async Task GetAllPagedAsync_FilterByType_ReturnsOnlyMatchingType()
    {
        _context.Transactions.AddRange(
            new Transaction { ProductId = Guid.NewGuid(), Type = TransactionType.Sale, Quantity = 1, UnitPrice = 10m, TotalPrice = 10m },
            new Transaction { ProductId = Guid.NewGuid(), Type = TransactionType.Purchase, Quantity = 2, UnitPrice = 5m, TotalPrice = 10m }
        );
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(It.IsAny<Guid>())).ReturnsAsync(MakeProduct());

        var result = await _service.GetAllPagedAsync(new TransactionFilterDto { Type = "Sale", Page = 1, PageSize = 10 });

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Data, t => Assert.Equal("Sale", t.Type));
    }

    [Fact]
    public async Task GetAllPagedAsync_FilterByDateRange_ReturnsMatchingTransactions()
    {
        var yesterday = DateTimeOffset.UtcNow.AddDays(-1);
        var lastWeek = DateTimeOffset.UtcNow.AddDays(-7);
        var lastMonth = DateTimeOffset.UtcNow.AddDays(-30);

        _context.Transactions.AddRange(
            new Transaction { ProductId = Guid.NewGuid(), Type = TransactionType.Purchase, Quantity = 1, UnitPrice = 10m, TotalPrice = 10m, Date = yesterday },
            new Transaction { ProductId = Guid.NewGuid(), Type = TransactionType.Purchase, Quantity = 1, UnitPrice = 10m, TotalPrice = 10m, Date = lastMonth }
        );
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(It.IsAny<Guid>())).ReturnsAsync(MakeProduct());

        var result = await _service.GetAllPagedAsync(new TransactionFilterDto
        {
            DateFrom = lastWeek,
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllPagedAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 12; i++)
            _context.Transactions.Add(new Transaction
            {
                ProductId = Guid.NewGuid(), Type = TransactionType.Purchase,
                Quantity = 1, UnitPrice = i * 10m, TotalPrice = i * 10m
            });
        await _context.SaveChangesAsync();

        _productClientMock.Setup(c => c.GetProductAsync(It.IsAny<Guid>())).ReturnsAsync(MakeProduct());

        var result = await _service.GetAllPagedAsync(new TransactionFilterDto { Page = 2, PageSize = 5 });

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(5, result.Data.Count());
    }
}
