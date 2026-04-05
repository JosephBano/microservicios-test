using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TransactionService.Controllers;
using TransactionService.DTOs;
using TransactionService.Services;
using Xunit;

namespace TransactionService.Tests;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _serviceMock = new();
    private readonly Mock<IValidator<CreateTransactionDto>> _createValidator = new();
    private readonly Mock<IValidator<UpdateTransactionDto>> _updateValidator = new();
    private readonly TransactionsController _controller;

    public TransactionsControllerTests()
    {
        _controller = new TransactionsController(_serviceMock.Object, _createValidator.Object, _updateValidator.Object);
    }

    private static ValidationResult Valid() => new ValidationResult();
    private static ValidationResult Invalid(string msg) =>
        new ValidationResult(new[] { new ValidationFailure("field", msg) });

    private static TransactionResponseDto MakeTransactionDto(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), DateTimeOffset.UtcNow, "Purchase", Guid.NewGuid(),
            "Laptop", 15, 5, 99.99m, 499.95m, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    // 
    // GET /api/transactions
    // 

    [Fact]
    public async Task GetAll_ReturnsOkWithPagedResult()
    {
        var paged = new PagedResponseDto<TransactionResponseDto>([], 0, 1, 10, 0);
        _serviceMock.Setup(s => s.GetAllPagedAsync(It.IsAny<TransactionFilterDto>())).ReturnsAsync(paged);

        var result = await _controller.GetAll(new TransactionFilterDto());

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(paged, ok.Value);
    }

    // 
    // GET /api/transactions/{id}
    // 

    [Fact]
    public async Task GetById_ExistingId_ReturnsOk()
    {
        var dto = MakeTransactionDto();
        _serviceMock.Setup(s => s.GetByIdAsync(dto.Id)).ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(dto, ok.Value);
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((TransactionResponseDto?)null);

        var result = await _controller.GetById(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // 
    // GET /api/transactions/product/{productId}
    // 

    [Fact]
    public async Task GetByProduct_ReturnsOkWithPagedResult()
    {
        var productId = Guid.NewGuid();
        var paged = new PagedResponseDto<TransactionResponseDto>([], 0, 1, 10, 0);
        _serviceMock.Setup(s => s.GetByProductIdAsync(productId, 1, 10)).ReturnsAsync(paged);

        var result = await _controller.GetByProduct(productId, 1, 10);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(paged, ok.Value);
    }

    // 
    // POST /api/transactions
    // 

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 5, 99m, null, null);
        var created = MakeTransactionDto();
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.CreateAsync(dto)).ReturnsAsync((created, (string?)null));

        var result = await _controller.Create(dto);

        Assert.IsType<CreatedAtActionResult>(result.Result);
    }

    [Fact]
    public async Task Create_InvalidDto_ReturnsBadRequest()
    {
        var dto = new CreateTransactionDto("", Guid.Empty, 0, -1m, null, null);
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Invalid("Tipo requerido"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_InsufficientStock_ReturnsUnprocessableEntity()
    {
        var dto = new CreateTransactionDto("Sale", Guid.NewGuid(), 100, 99m, null, null);
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.CreateAsync(dto))
            .ReturnsAsync(((TransactionResponseDto?)null, "Stock insuficiente. Disponible: 5, solicitado: 100"));

        var result = await _controller.Create(dto);

        Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
    }

    [Fact]
    public async Task Create_ProductNotFound_ReturnsBadRequest()
    {
        var dto = new CreateTransactionDto("Purchase", Guid.NewGuid(), 1, 10m, null, null);
        _createValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.CreateAsync(dto))
            .ReturnsAsync(((TransactionResponseDto?)null, "Producto no encontrado"));

        var result = await _controller.Create(dto);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // 
    // PUT /api/transactions/{id}
    // 

    [Fact]
    public async Task Update_ExistingTransaction_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateTransactionDto("Updated detail", null);
        var updated = MakeTransactionDto(id);
        _updateValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _controller.Update(id, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(updated, ok.Value);
    }

    [Fact]
    public async Task Update_NonExistingTransaction_ReturnsNotFound()
    {
        var dto = new UpdateTransactionDto(null, null);
        _updateValidator.Setup(v => v.ValidateAsync(dto, default)).ReturnsAsync(Valid());
        _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<Guid>(), dto)).ReturnsAsync((TransactionResponseDto?)null);

        var result = await _controller.Update(Guid.NewGuid(), dto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    // 
    // DELETE /api/transactions/{id}
    // 

    [Fact]
    public async Task Delete_ExistingTransaction_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync((true, (string?)null));

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_NonExistingTransaction_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync((false, (string?)null));

        var result = await _controller.Delete(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_StockRevertFailed_ReturnsInternalServerError()
    {
        var id = Guid.NewGuid();
        _serviceMock.Setup(s => s.DeleteAsync(id)).ReturnsAsync((false, "No se pudo revertir el stock"));

        var result = await _controller.Delete(id);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }
}
