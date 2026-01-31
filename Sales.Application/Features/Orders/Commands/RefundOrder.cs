using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record RefundOrderCommand(Guid OrderId, RefundOrderRequest Request) : IRequest;

public sealed class RefundOrderRequest
{
    public string? Reason { get; init; }
    public decimal? Amount { get; init; }
    public string? Note { get; init; }
}
