using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record CompleteRefundCommand(Guid RefundId, CompleteRefundRequest Request) : IRequest;

public sealed class CompleteRefundRequest
{
    public string? WalletReference { get; init; }
}
