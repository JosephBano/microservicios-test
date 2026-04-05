using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ProductService.Controllers;
using ProductService.DTOs;
using ProductService.Services;
using Xunit;

namespace ProductService.Tests;

public class ProductsControllerTests
{
    private readonly Mock<IProductService> _serviceMock = new();
    private readonly Mock<IValidator<CreateProductDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateProductDto>> _updateValidator = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_serviceMock.Object, _createValidator.Object, _updateValidator.Object);
    }

    private static ValidationResult Valid() => new ValidationResult();
    private static ValidationResult Invalid(string msg) =>
        new ValidationResult(new[] { new ValidationFailure("field", msg) });

    // 
    // GET /api/products
    // 

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var pagedResult = new PagedResponseDto<ProductResponseDto>([], 0, 1, 10, 0);
        _serviceMock.Setup(s => s.GetAllPagedAsync(It.IsAny<ProductFilterDto>())).ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(new ProductFilterDto());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(pagedResult, ok.Value);
    }

    // 
    // GET /api/products/{id}
    // 

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new ProductResponseDto(id, "Laptop", null, "Elec", null, 999m, 10, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _serviceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(dto);

        var result = await _controller.GetById(id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((ProductResponseDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // 
    // POST /api/products
    // 

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateProductDto("Laptop", null, "Elec", null, 999m, 10);
        var created = new ProductResponseDto(Guid.NewGuid(), "Laptop", null, "Elec", null, 999m, 10, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _controller.Create(dto);

        var createdAt = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(created, createdAt.Value);
    }

    [Fact]
    public async Task Create_InvalidDto_ReturnsBadRequest()
    {
        var dto = new CreateProductDto("", null, "", null, -1m, -1);
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Invalid("Nombre requerido"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // 
    // PUT /api/products/{id}
    // 

    [Fact]
    public async Task Update_ExistingProduct_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateProductDto("Updated", null, "Cat", null, 50m, 5);
        var updated = new ProductResponseDto(id, "Updated", null, "Cat", null, 50m, 5, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        _updateValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _controller.Update(id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_NonExistingProduct_ReturnsNotFound()
    {
        var dto = new UpdateProductDto("X", null, "Y", null, 1m, 1);
        _updateValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), dto)).ReturnsAsync((ProductResponseDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // 
    // DELETE /api/products/{id}
    // 

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingProduct_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // 
    // PATCH /api/products/{id}/stock
    // 

    [Fact]
    public async Task UpdateStock_Success_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new StockUpdateRequestDto(-5, "Sale");
        var response = new StockUpdateResponseDto(id, "Laptop", 5);
        _serviceMock.Setup(s => s.UpdateStockAsync(id, dto)).ReturnsAsync(response);

        var result = await _controller.UpdateStock(id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(response, ok.Value);
    }

    [Fact]
    public async Task UpdateStock_InsufficientStock_ReturnsConflict()
    {
        var id = Guid.NewGuid();
        var dto = new StockUpdateRequestDto(-100, "Sale");
        _serviceMock.Setup(s => s.UpdateStockAsync(id, dto))
            .ThrowsAsync(new InvalidOperationException("Stock insuficiente"));

        var result = await _controller.UpdateStock(id, dto);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStock_NonExistingProduct_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.UpdateStockAsync(It.IsAny<Guid>(), It.IsAny<StockUpdateRequestDto>()))
            .ReturnsAsync((StockUpdateResponseDto?)null);

        var result = await _controller.UpdateStock(Guid.NewGuid(), new StockUpdateRequestDto(1, "Purchase"));

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }
}
