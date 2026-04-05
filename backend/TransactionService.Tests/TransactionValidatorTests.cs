using TransactionService.DTOs;
using TransactionService.Validators;
using Xunit;

namespace TransactionService.Tests;

public class TransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    [Fact]
    public async Task Create_ValidPurchaseDto_PassesValidation()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 5, 99.99m, "Compra mensual", null);
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_ValidSaleDto_PassesValidation()
    {
        var dto = new CreateTransactionDto("Sale", Guid.NewGuid(), 2, 149.00m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_EmptyType_FailsValidation()
    {
        var dto = new CreateTransactionDto("", Guid.NewGuid(), 1, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Create_InvalidType_FailsValidation()
    {
        var dto = new CreateTransactionDto("Return", Guid.NewGuid(), 1, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Type");
    }

    [Fact]
    public async Task Create_EmptyProductId_FailsValidation()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.Empty, 1, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public async Task Create_ZeroQuantity_FailsValidation()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 0, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task Create_NegativeQuantity_FailsValidation()
    {
        var dto = new CreateTransactionDto("Sale", Guid.NewGuid(), -3, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public async Task Create_NegativeUnitPrice_FailsValidation()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 1, -5m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
    }

    [Fact]
    public async Task Create_ZeroUnitPrice_PassesValidation()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 1, 0m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("purchase")]
    [InlineData("sale")]
    [InlineData("PURCHASE")]
    [InlineData("SALE")]
    public async Task Create_TypeCaseInsensitive_PassesValidation(string type)
    {
        var dto = new CreateTransactionDto(type, Guid.NewGuid(), 1, 10m, null, null);
        var result = await _validator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }
}
