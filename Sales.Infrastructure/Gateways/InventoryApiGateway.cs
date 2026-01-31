using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sales.Application.Abstractions;

namespace Sales.Infrastructure.Gateways;

public sealed class InventoryApiGateway : IInventoryReservationGateway
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

    public async Task ReserveAsync(Guid orderId, IReadOnlyList<InventoryReservationRequestLine> lines, CancellationToken ct)
    {
        var body = new ReservationRequestDto
        {
            OrderId = orderId,
            Lines = lines.Select(l => new ReservationLineDto { SkuId = l.SkuId, Qty = l.Qty }).ToList()
        };

        var payload = JsonSerializer.Serialize(body, JsonOptions);
        var response = await _httpClient.PostAsync(
            "/api/inventory/reservations",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Inventory reservation endpoint not found.");

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Inventory reservation failed: {StatusCode}. Body: {Body}", (int)response.StatusCode, text);
            var msg = ExtractMessage(text)
                ?? $"Inventory reservation failed. Status: {(int)response.StatusCode}";
            throw new InvalidOperationException(msg);
        }
    }

    public async Task ReleaseAsync(Guid orderId, CancellationToken ct)
    {
        var response = await _httpClient.PostAsync(
            $"/api/inventory/reservations/{orderId}/release",
            content: null,
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Inventory reservation release endpoint not found.");

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Inventory reservation release failed: {StatusCode}. Body: {Body}", (int)response.StatusCode, text);
            var msg = ExtractMessage(text)
                ?? $"Inventory reservation release failed. Status: {(int)response.StatusCode}";
            throw new InvalidOperationException(msg);
        }
    }

    private sealed class ReservationRequestDto
    {
        public Guid OrderId { get; init; }
        public List<ReservationLineDto> Lines { get; init; } = new();
    }

    private sealed class ReservationLineDto
    {
        public string SkuId { get; init; } = string.Empty;
        public decimal Qty { get; init; }
    }

    private static string? ExtractMessage(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
                if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    return detail.GetString();
                if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                    return title.GetString();
                if (doc.RootElement.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                    return err.GetString();
            }
        }
        catch
        {
            // ignore parse errors
        }

        var trimmed = raw.Trim();
        if (trimmed.Length > 500) trimmed = trimmed[..500] + "...";
        return trimmed;
    }
}
