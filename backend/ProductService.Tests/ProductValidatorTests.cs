using ProductService.DTOs;
using ProductService.Validators;
using Xunit;

namespace ProductService.Tests;

public class ProductValidatorTests
{
    private readonly CreateProductValidator _createValidator = new();
    private readonly UpdateProductValidator _updateValidator = new();

    // 
    // CreateProductValidator
    // 

    [Fact]
    public async Task Create_ValidDto_PassesValidation()
    {
        var dto = new CreateProductDto("Laptop", "Desc", "Elec", "https://img.com/a.jpg", 999m, 10);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_EmptyName_FailsValidation()
    {
        var dto = new CreateProductDto("", null, "Elec", null, 10m, 5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Create_EmptyCategory_FailsValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "", null, 10m, 5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Category");
    }

    [Fact]
    public async Task Create_NegativePrice_FailsValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", null, -1m, 5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Price");
    }

    [Fact]
    public async Task Create_NegativeStock_FailsValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", null, 10m, -5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Stock");
    }

    [Fact]
    public async Task Create_InvalidImageUrl_FailsValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", "not-a-url", 10m, 5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ImageUrl");
    }

    [Fact]
    public async Task Create_ZeroPriceAndStock_PassesValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", null, 0m, 0);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Create_NullImageUrl_PassesValidation()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", null, 10m, 5);
        var result = await _createValidator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    // 
    // UpdateProductValidator
    // 

    [Fact]
    public async Task Update_ValidDto_PassesValidation()
    {
        var dto = new UpdateProductDto("Laptop", "Desc", "Elec", null, 999m, 10);
        var result = await _updateValidator.ValidateAsync(dto);
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Update_EmptyName_FailsValidation()
    {
        var dto = new UpdateProductDto("", null, "Elec", null, 10m, 5);
        var result = await _updateValidator.ValidateAsync(dto);
        Assert.False(result.IsValid);
    }
}
