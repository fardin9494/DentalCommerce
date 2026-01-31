using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record CancelOrderCommand(Guid OrderId, CancelOrderRequest Request) : IRequest;

public sealed class CancelOrderRequest
{
    public string? Reason { get; init; }
    public string? Note { get; init; }
}
