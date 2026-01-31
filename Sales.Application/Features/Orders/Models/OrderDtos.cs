namespace Sales.Application.Features.Orders.Models;

public sealed class OrderNoteDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string Note { get; init; } = null!;
    public string? CreatedBy { get; init; }
    public bool IsInternal { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string OrderNumber { get; init; } = null!;
    public Guid PricingQuoteId { get; init; }
    public string Currency { get; init; } = "IRR";
    public string Status { get; init; } = "Draft";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? PlacedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public DateTime? PaymentFailedAtUtc { get; init; }
    public string? PaymentFailureReason { get; init; }
    public string? PaymentFailureDetails { get; init; }

    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal FinalTotal { get; init; }
    public decimal CashbackTotal { get; init; }

    public List<OrderLineDto> Lines { get; init; } = new();
}

public sealed class OrderLineDto
{
    public string SkuId { get; init; } = null!;
    public Guid? BatchId { get; init; }
    public int Quantity { get; init; }
    public decimal BaseUnitPrice { get; init; }
    public decimal FinalUnitPrice { get; init; }
    public bool IsGift { get; init; }
}

public sealed class OrderListResult
{
    public IReadOnlyList<OrderListItemDto> Items { get; init; } = Array.Empty<OrderListItemDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed class OrderListItemDto
{
    public Guid Id { get; init; }
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string? SiteName { get; init; }
    public string? SiteDomain { get; init; }
    public string OrderNumber { get; init; } = null!;
    public Guid PricingQuoteId { get; init; }
    public string Currency { get; init; } = "IRR";
    public string Status { get; init; } = "Draft";
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? PlacedAtUtc { get; init; }
    public DateTime? PaymentFailedAtUtc { get; init; }
    public string? PaymentFailureReason { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal FinalTotal { get; init; }
    public decimal CashbackTotal { get; init; }
    public int LinesCount { get; init; }
}
