using System.Net;
using System.Text.Json;
using Pricing.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Pricing.Infrastructure.Gateways;

public sealed class InventoryApiGateway : IInventoryBatchInfoGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryApiGateway> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InventoryApiGateway(IHttpClientFactory httpClientFactory, ILogger<InventoryApiGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient("InventoryApi");
        _logger = logger;
    }

    public async Task<BatchInfo?> GetBatchInfoAsync(string skuId, Guid batchId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/inventory/batches/{batchId}?skuId={Uri.EscapeDataString(skuId)}", ct);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<BatchInfoResponse>(json, JsonOptions);
            if (dto is null) return null;

            if (!string.Equals(dto.SkuId, skuId, StringComparison.OrdinalIgnoreCase))
                return null;

            return new BatchInfo(dto.BatchId, dto.SkuId, dto.ExpiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate batch {BatchId} for SKU {SkuId}", batchId, skuId);
            return null;
        }
    }

    private sealed class BatchInfoResponse
    {
        public Guid BatchId { get; init; }
        public string SkuId { get; init; } = string.Empty;
        public DateTime? ExpiryDate { get; init; }
    }
}
