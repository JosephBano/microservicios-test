using ProductService.DTOs;

namespace ProductService.Services;

public interface IProductService
{
    Task<PagedResponseDto<ProductResponseDto>> GetAllPagedAsync(ProductFilterDto filter);
    Task<ProductResponseDto?> GetByIdAsync(Guid id);
    Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
    Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<StockUpdateResponseDto?> UpdateStockAsync(Guid id, StockUpdateRequestDto dto);
}
