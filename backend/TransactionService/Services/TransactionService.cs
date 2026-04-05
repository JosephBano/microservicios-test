using Microsoft.EntityFrameworkCore;
using TransactionService.Data;
using TransactionService.DTOs;
using TransactionService.Models;

namespace TransactionService.Services;

public class TransactionService : ITransactionService
{
    private readonly TransactionDbContext _context;
    private readonly IProductServiceClient _productClient;

    public TransactionService(TransactionDbContext context, IProductServiceClient productClient)
    {
        _context = context;
        _productClient = productClient;
    }

    public async Task<PagedResponseDto<TransactionResponseDto>> GetAllPagedAsync(TransactionFilterDto filter)
    {
        var query = _context.Transactions.AsQueryable();

        if (filter.ProductId.HasValue)
            query = query.Where(t => t.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Type) && Enum.TryParse<TransactionType>(filter.Type, true, out var type))
            query = query.Where(t => t.Type == type);

        if (filter.DateFrom.HasValue)
            query = query.Where(t => t.Date >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(t => t.Date <= filter.DateTo.Value);

        var totalCount = await query.CountAsync();
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);
        var page = Math.Max(filter.Page, 1);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var transactions = await query
            .OrderByDescending(t => t.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Enriquecer con datos del producto
        var dtos = await EnrichWithProductDataAsync(transactions);

        return new PagedResponseDto<TransactionResponseDto>(dtos, totalCount, page, pageSize, totalPages);
    }

    public async Task<TransactionResponseDto?> GetByIdAsync(Guid id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction is null) return null;

        var product = await TryGetProductAsync(transaction.ProductId);
        return MapToDto(transaction, product);
    }

    public async Task<(TransactionResponseDto? Transaction, string? Error)> CreateAsync(CreateTransactionDto dto)
    {
        if (!Enum.TryParse<TransactionType>(dto.Type, true, out var transactionType))
            return (null, $"Tipo de transacción inválido: {dto.Type}. Use 'Purchase' o 'Sale'.");

        // 1. Verificar que el producto existe y obtener stock actual
        ProductDto? product;
        try
        {
            product = await _productClient.GetProductAsync(dto.ProductId);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }

        if (product is null)
            return (null, $"Producto con ID {dto.ProductId} no encontrado.");

        // 2. Validar stock si es una venta
        if (transactionType == TransactionType.Sale && product.Stock < dto.Quantity)
            return (null, $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {dto.Quantity}.");

        // 3. Ajustar stock en ProductService
        var adjustment = transactionType == TransactionType.Sale ? -dto.Quantity : dto.Quantity;
        bool stockUpdated;
        try
        {
            stockUpdated = await _productClient.UpdateStockAsync(dto.ProductId, adjustment, dto.Type);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }

        if (!stockUpdated)
            return (null, "No se pudo actualizar el stock. Puede que el stock sea insuficiente.");

        // 4. Persistir la transacción
        var transaction = new Transaction
        {
            Date = dto.Date ?? DateTimeOffset.UtcNow,
            Type = transactionType,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TotalPrice = dto.Quantity * dto.UnitPrice,
            Detail = dto.Detail
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Recargar producto para obtener stock actualizado
        var updatedProduct = await TryGetProductAsync(dto.ProductId);
        return (MapToDto(transaction, updatedProduct), null);
    }

    public async Task<TransactionResponseDto?> UpdateAsync(Guid id, UpdateTransactionDto dto)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction is null) return null;

        if (dto.Detail is not null) transaction.Detail = dto.Detail;
        if (dto.Date.HasValue) transaction.Date = dto.Date.Value;
        transaction.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        var product = await TryGetProductAsync(transaction.ProductId);
        return MapToDto(transaction, product);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction is null) return (false, null);

        // Revertir el ajuste de stock
        var reverseAdjustment = transaction.Type == TransactionType.Sale
            ? transaction.Quantity
            : -transaction.Quantity;

        try
        {
            await _productClient.UpdateStockAsync(transaction.ProductId, reverseAdjustment, transaction.Type.ToString());
        }
        catch (InvalidOperationException ex)
        {
            return (false, $"No se pudo revertir el stock: {ex.Message}");
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<PagedResponseDto<TransactionResponseDto>> GetByProductIdAsync(Guid productId, int page, int pageSize)
    {
        var filter = new TransactionFilterDto { ProductId = productId, Page = page, PageSize = pageSize };
        return await GetAllPagedAsync(filter);
    }

    private async Task<List<TransactionResponseDto>> EnrichWithProductDataAsync(List<Transaction> transactions)
    {
        var productIds = transactions.Select(t => t.ProductId).Distinct().ToList();
        var productCache = new Dictionary<Guid, ProductDto?>();

        foreach (var pid in productIds)
            productCache[pid] = await TryGetProductAsync(pid);

        return transactions.Select(t =>
        {
            productCache.TryGetValue(t.ProductId, out var product);
            return MapToDto(t, product);
        }).ToList();
    }

    private async Task<ProductDto?> TryGetProductAsync(Guid productId)
    {
        try { return await _productClient.GetProductAsync(productId); }
        catch { return null; }
    }

    private static TransactionResponseDto MapToDto(Transaction t, ProductDto? product) =>
        new(
            t.Id,
            t.Date,
            t.Type.ToString(),
            t.ProductId,
            product?.Name,
            product?.Stock,
            t.Quantity,
            t.UnitPrice,
            t.TotalPrice,
            t.Detail,
            t.CreatedAt,
            t.UpdatedAt
        );
}
