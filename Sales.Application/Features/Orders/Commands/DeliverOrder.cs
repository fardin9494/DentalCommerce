using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record DeliverOrderCommand(Guid OrderId, DeliverOrderRequest Request) : IRequest;

public sealed class DeliverOrderRequest
{
    public string? Note { get; init; }
}
