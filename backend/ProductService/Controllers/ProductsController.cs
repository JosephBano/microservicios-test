using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Services;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;

    public ProductsController(
        IProductService productService,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator)
    {
        _productService = productService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Lista productos con paginación y filtros dinámicos.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<ProductResponseDto>>> GetAll([FromQuery] ProductFilterDto filter)
    {
        var result = await _productService.GetAllPagedAsync(filter);
        return Ok(result);
    }

    /// <summary>Obtiene un producto por su ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });
        return Ok(product);
    }

    /// <summary>Crea un nuevo producto.</summary>
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> Create([FromBody] CreateProductDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var product = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>Actualiza un producto existente.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var product = await _productService.UpdateAsync(id, dto);
        if (product is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });
        return Ok(product);
    }

    /// <summary>Elimina un producto.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _productService.DeleteAsync(id);
        if (!deleted) return NotFound(new { message = $"Producto con ID {id} no encontrado." });
        return NoContent();
    }

    /// <summary>Ajusta el stock de un producto. Uso interno de TransactionService.</summary>
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<StockUpdateResponseDto>> UpdateStock(Guid id, [FromBody] StockUpdateRequestDto dto)
    {
        try
        {
            var result = await _productService.UpdateStockAsync(id, dto);
            if (result is null) return NotFound(new { message = $"Producto con ID {id} no encontrado." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                error = "InsufficientStock",
                message = ex.Message
            });
        }
    }
}
