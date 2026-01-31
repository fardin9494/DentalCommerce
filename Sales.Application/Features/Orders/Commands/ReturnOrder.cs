using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record ReturnOrderCommand(Guid OrderId, ReturnOrderRequest Request) : IRequest;

public sealed class ReturnOrderRequest
{
    public string? Reason { get; init; }
    public string? Note { get; init; }
}
