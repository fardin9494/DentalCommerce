using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record CreateOrderCommand(CreateOrderRequest Request) : IRequest<CreateOrderResult>;

public sealed class CreateOrderRequest
{
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string? CouponCode { get; init; }
    public List<CreateOrderItem> Items { get; init; } = new();
}

public sealed class CreateOrderItem
{
    public string SkuId { get; init; } = null!;
    public int Qty { get; init; }
    public Guid? BatchId { get; init; }
}

public sealed record CreateOrderResult(
    Guid OrderId,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal FinalTotal,
    decimal CashbackTotal);

