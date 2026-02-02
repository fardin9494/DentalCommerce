using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record ApproveRefundCommand(Guid RefundId, ApproveRefundRequest Request) : IRequest;

public sealed class ApproveRefundRequest
{
    public string? Note { get; init; }
}
