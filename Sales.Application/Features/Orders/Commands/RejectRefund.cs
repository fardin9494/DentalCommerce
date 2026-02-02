using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record RejectRefundCommand(Guid RefundId, RejectRefundRequest Request) : IRequest;

public sealed class RejectRefundRequest
{
    public string Reason { get; init; } = null!;
}
