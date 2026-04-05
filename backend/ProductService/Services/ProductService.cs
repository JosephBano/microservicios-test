using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Services;

public class ProductService : IProductService
{
    private readonly ProductDbContext _context;

    public ProductService(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponseDto<ProductResponseDto>> GetAllPagedAsync(ProductFilterDto filter)
    {
        var query = _context.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(p => p.Name.ToLower().Contains(filter.Name.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(p => p.Category.ToLower().Contains(filter.Category.ToLower()));

        if (filter.MinPrice.HasValue)
            query = query.Where(p => p.Price >= filter.MinPrice.Value);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);

        if (filter.MinStock.HasValue)
            query = query.Where(p => p.Stock >= filter.MinStock.Value);

        var totalCount = await query.CountAsync();
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);
        var page = Math.Max(filter.Page, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return new PagedResponseDto<ProductResponseDto>(products, totalCount, page, pageSize, totalPages);
    }

    public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        return product is null ? null : MapToDto(product);
    }

    public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            Description = dto.Description,
            Category = dto.Category,
            ImageUrl = dto.ImageUrl,
            Price = dto.Price,
            Stock = dto.Stock
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return null;

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Category = dto.Category;
        product.ImageUrl = dto.ImageUrl;
        product.Price = dto.Price;
        product.Stock = dto.Stock;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StockUpdateResponseDto?> UpdateStockAsync(Guid id, StockUpdateRequestDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return null;

        var newStock = product.Stock + dto.Adjustment;
        if (newStock < 0)
            throw new InvalidOperationException(
                $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {Math.Abs(dto.Adjustment)}");

        product.Stock = newStock;
        product.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return new StockUpdateResponseDto(product.Id, product.Name, product.Stock);
    }

    private static ProductResponseDto MapToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Category, p.ImageUrl, p.Price, p.Stock, p.CreatedAt, p.UpdatedAt);
}
