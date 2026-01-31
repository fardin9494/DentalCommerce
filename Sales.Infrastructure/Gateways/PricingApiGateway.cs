using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sales.Application.Abstractions;

namespace Sales.Infrastructure.Gateways;

public sealed class PricingApiGateway : IPricingQuoteGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PricingApiGateway> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PricingApiGateway(IHttpClientFactory httpClientFactory, ILogger<PricingApiGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient("PricingApi");
        _logger = logger;
    }

    public async Task<PricingQuoteSnapshot> CreateQuoteAsync(PricingQuoteRequest request, CancellationToken ct)
    {
        var body = new QuoteRequestDto
        {
            SiteId = request.SiteId,
            UserId = request.UserId,
            Timestamp = null,
            CouponCode = request.CouponCode,
            Items = request.Items.Select(i => new QuoteRequestItemDto
            {
                SkuId = i.SkuId,
                Qty = i.Qty,
                BatchId = i.BatchId
            }).ToList()
        };

        var payload = JsonSerializer.Serialize(body, JsonOptions);
        var response = await _httpClient.PostAsync(
            "/api/pricing/quote",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new InvalidOperationException("Pricing quote endpoint not found.");

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Pricing quote failed: {StatusCode}. Body: {Body}", (int)response.StatusCode, text);
            var msg = ExtractMessage(text)
                ?? $"Pricing quote failed. Status: {(int)response.StatusCode}";
            throw new InvalidOperationException(msg);
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var dto = JsonSerializer.Deserialize<QuoteResponseDto>(json, JsonOptions)
                  ?? throw new InvalidOperationException("Pricing quote response is invalid.");

        var ts = dto.Timestamp.Kind == DateTimeKind.Utc
            ? dto.Timestamp
            : DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc);

        return new PricingQuoteSnapshot(
            dto.Id,
            dto.SiteId,
            dto.UserId,
            dto.Currency,
            ts,
            dto.Subtotal,
            dto.DiscountTotal,
            dto.FinalTotal,
            dto.CashbackTotal,
            dto.Lines.Select(l => new PricingQuoteLineSnapshot(
                l.SkuId,
                l.BatchId,
                l.Quantity,
                l.BaseUnitPrice,
                l.FinalUnitPrice,
                l.IsGift,
                l.Adjustments.ValueKind == JsonValueKind.Undefined ? null : l.Adjustments.GetRawText())).ToList());
    }

    private sealed class QuoteRequestDto
    {
        public Guid SiteId { get; init; }
        public Guid? UserId { get; init; }
        public DateTime? Timestamp { get; init; }
        public string? CouponCode { get; init; }
        public List<QuoteRequestItemDto> Items { get; init; } = new();
    }

    private sealed class QuoteRequestItemDto
    {
        public string SkuId { get; init; } = string.Empty;
        public int Qty { get; init; }
        public Guid? BatchId { get; init; }
    }

    private sealed class QuoteResponseDto
    {
        public Guid Id { get; init; }
        public Guid SiteId { get; init; }
        public Guid? UserId { get; init; }
        public string Currency { get; init; } = "IRR";
        public DateTime Timestamp { get; init; }

        public List<QuoteLineResponseDto> Lines { get; init; } = new();
        public decimal Subtotal { get; init; }
        public decimal DiscountTotal { get; init; }
        public decimal FinalTotal { get; init; }
        public decimal CashbackTotal { get; init; }
    }

    private sealed class QuoteLineResponseDto
    {
        public string SkuId { get; init; } = string.Empty;
        public Guid? BatchId { get; init; }
        public int Quantity { get; init; }
        public decimal BaseUnitPrice { get; init; }
        public decimal FinalUnitPrice { get; init; }
        public bool IsGift { get; init; }
        public JsonElement Adjustments { get; init; }
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
