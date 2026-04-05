using TransactionService.DTOs;

namespace TransactionService.Services;

public interface IProductServiceClient
{
    Task<ProductDto?> GetProductAsync(Guid productId);
    Task<bool> UpdateStockAsync(Guid productId, int adjustment, string transactionType);
}
