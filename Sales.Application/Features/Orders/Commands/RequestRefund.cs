using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record RequestRefundCommand(Guid OrderId, RequestRefundRequest Request) : IRequest<RequestRefundResult>;

public sealed class RequestRefundRequest
{
    public List<RequestRefundLineItem> Lines { get; init; } = new();
    public string? Reason { get; init; }
    public string? Note { get; init; }
    public string? RequestedBy { get; init; }
}

public sealed class RequestRefundLineItem
{
    public string SkuId { get; init; } = null!;
    public Guid? BatchId { get; init; }
    public int Qty { get; init; }
}

public sealed record RequestRefundResult(
    Guid RefundId,
    decimal Amount,
    string Currency);
