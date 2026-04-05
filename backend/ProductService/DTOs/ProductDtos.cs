namespace ProductService.DTOs;

public record ProductResponseDto(
    Guid Id,
    string Name,
    string? Description,
    string Category,
    string? ImageUrl,
    decimal Price,
    int Stock,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public record CreateProductDto(
    string Name,
    string? Description,
    string Category,
    string? ImageUrl,
    decimal Price,
    int Stock
);

public record UpdateProductDto(
    string Name,
    string? Description,
    string Category,
    string? ImageUrl,
    decimal Price,
    int Stock
);

public record StockUpdateRequestDto(
    int Adjustment,
    string TransactionType
);

public record StockUpdateResponseDto(
    Guid Id,
    string Name,
    int NewStock
);

public record PagedResponseDto<T>(
    IEnumerable<T> Data,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public record ProductFilterDto
{
    public string? Name { get; init; }
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinStock { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
