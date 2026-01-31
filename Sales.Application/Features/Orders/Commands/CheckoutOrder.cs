using MediatR;
using Sales.Application.Abstractions;

namespace Sales.Application.Features.Orders.Commands;

public sealed record CheckoutOrderCommand(CheckoutOrderRequest Request) : IRequest<CheckoutOrderResult>;

public sealed class CheckoutOrderRequest
{
    public Guid SiteId { get; init; }
    public Guid? UserId { get; init; }
    public string? CouponCode { get; init; }
    public PaymentScenario PaymentScenario { get; init; } = PaymentScenario.Success;
    public List<CheckoutOrderItem> Items { get; init; } = new();
}

public sealed class CheckoutOrderItem
{
    public string SkuId { get; init; } = null!;
    public int Qty { get; init; }
    public Guid? BatchId { get; init; }
}

public sealed record CheckoutOrderResult(
    Guid OrderId,
    string Status,
    string Currency,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal FinalTotal,
    decimal CashbackTotal,
    string? FailureReason = null);

