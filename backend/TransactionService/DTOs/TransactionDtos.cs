using TransactionService.Models;

namespace TransactionService.DTOs;

public record TransactionResponseDto(
    Guid Id,
    DateTimeOffset Date,
    string Type,
    Guid ProductId,
    string? ProductName,
    int? ProductStock,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? Detail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CreateTransactionDto(
    string Type,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    string? Detail,
    DateTimeOffset? Date
);

public record UpdateTransactionDto(
    string? Detail,
    DateTimeOffset? Date
);

public record PagedResponseDto<T>(
    IEnumerable<T> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record TransactionFilterDto
{
    public Guid? ProductId { get; init; }
    public string? Type { get; init; }
    public DateTimeOffset? DateFrom { get; init; }
    public DateTimeOffset? DateTo { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

// DTOs para comunicación con ProductService
public record ProductDto(
    Guid Id,
    string Name,
    string Category,
    decimal Price,
    int Stock
);

public record StockUpdateRequestDto(
    int Adjustment,
    string TransactionType
);
