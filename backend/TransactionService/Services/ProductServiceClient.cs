using System.Net.Http.Json;
using TransactionService.DTOs;

namespace TransactionService.Services;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductServiceClient> _logger;

    public ProductServiceClient(HttpClient httpClient, ILogger<ProductServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductDto?> GetProductAsync(Guid productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al comunicarse con ProductService para obtener producto {ProductId}", productId);
            throw new InvalidOperationException("El servicio de productos no está disponible.", ex);
        }
    }

    public async Task<bool> UpdateStockAsync(Guid productId, int adjustment, string transactionType)
    {
        try
        {
            var body = new StockUpdateRequestDto(adjustment, transactionType);
            var response = await _httpClient.PatchAsJsonAsync($"/api/products/{productId}/stock", body);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                return false;

            response.EnsureSuccessStatusCode();
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error al actualizar stock del producto {ProductId}", productId);
            throw new InvalidOperationException("El servicio de productos no está disponible.", ex);
        }
    }
}
