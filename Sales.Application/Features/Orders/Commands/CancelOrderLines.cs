using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record CancelOrderLinesCommand(Guid OrderId, CancelOrderLinesRequest Request) : IRequest<CancelOrderLinesResult>;

public sealed class CancelOrderLinesRequest
{
    public List<CancelOrderLineItem> Lines { get; init; } = new();
    public string? Reason { get; init; }
    public string? Note { get; init; }
    public string? RequestedBy { get; init; }
}

public sealed class CancelOrderLineItem
{
    public string SkuId { get; init; } = null!;
    public Guid? BatchId { get; init; }
    public int Qty { get; init; }
}

public sealed record CancelOrderLinesResult(
    Guid OrderId,
    Guid RefundId,
    decimal RefundAmount);
