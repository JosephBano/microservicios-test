using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TransactionService.DTOs;
using TransactionService.Services;

namespace TransactionService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly IValidator<CreateTransactionDto> _createValidator;
    private readonly IValidator<UpdateTransactionDto> _updateValidator;

    public TransactionsController(
        ITransactionService transactionService,
        IValidator<CreateTransactionDto> createValidator,
        IValidator<UpdateTransactionDto> updateValidator)
    {
        _transactionService = transactionService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Lista transacciones con paginación y filtros dinámicos.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<TransactionResponseDto>>> GetAll([FromQuery] TransactionFilterDto filter)
    {
        var result = await _transactionService.GetAllPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>Obtiene una transacción por su ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> GetById(Guid id)
    {
        var transaction = await _transactionService.GetByIdAsync(id);
        if (transaction is null) return NotFound(new { message = $"Transacción con ID {id} no encontrada." });
        return Ok(transaction);
    }

    /// <summary>Historial de transacciones de un producto específico.</summary>
    [HttpGet("product/{productId:guid}")]
    public async Task<ActionResult<PagedResponseDto<TransactionResponseDto>>> GetByProduct(
        Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _transactionService.GetByProductIdAsync(productId, page, pageSize);
        return Ok(result);
    }

    /// <summary>Crea una nueva transacción. Valida y ajusta el stock automáticamente.</summary>
    [HttpPost]
    public async Task<ActionResult<TransactionResponseDto>> Create([FromBody] CreateTransactionDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var (transaction, error) = await _transactionService.CreateAsync(dto);
        if (error is not null)
        {
            if (error.Contains("insuficiente") || error.Contains("Insuficiente"))
                return UnprocessableEntity(new { error = "InsufficientStock", message = error });
            if (error.Contains("no encontrado") || error.Contains("no disponible"))
                return BadRequest(new { error = "ServiceError", message = error });
            return BadRequest(new { error = "CreateFailed", message = error });
        }

        return CreatedAtAction(nameof(GetById), new { id = transaction!.Id }, transaction);
    }

    /// <summary>Edita el detalle y/o fecha de una transacción. No modifica cantidad ni tipo.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionResponseDto>> Update(Guid id, [FromBody] UpdateTransactionDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var transaction = await _transactionService.UpdateAsync(id, dto);
        if (transaction is null) return NotFound(new { message = $"Transacción con ID {id} no encontrada." });
        return Ok(transaction);
    }

    /// <summary>Elimina una transacción y revierte el ajuste de stock.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, error) = await _transactionService.DeleteAsync(id);
        if (!success && error is null) return NotFound(new { message = $"Transacción con ID {id} no encontrada." });
        if (error is not null) return StatusCode(500, new { message = error });
        return NoContent();
    }
}
