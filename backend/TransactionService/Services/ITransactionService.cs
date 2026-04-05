using TransactionService.DTOs;

namespace TransactionService.Services;

public interface ITransactionService
{
    Task<PagedResponseDto<TransactionResponseDto>> GetAllPagedAsync(TransactionFilterDto filter);
    Task<TransactionResponseDto?> GetByIdAsync(Guid id);
    Task<(TransactionResponseDto? Transaction, string? Error)> CreateAsync(CreateTransactionDto dto);
    Task<TransactionResponseDto?> UpdateAsync(Guid id, UpdateTransactionDto dto);
    Task<(bool Success, string? Error)> DeleteAsync(Guid id);
    Task<PagedResponseDto<TransactionResponseDto>> GetByProductIdAsync(Guid productId, int page, int pageSize);
}
