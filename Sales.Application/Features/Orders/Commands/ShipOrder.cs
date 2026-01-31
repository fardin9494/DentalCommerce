using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record ShipOrderCommand(Guid OrderId, ShipOrderRequest Request) : IRequest;

public sealed class ShipOrderRequest
{
    public string? Carrier { get; init; }
    public string? TrackingCode { get; init; }
    public string? Note { get; init; }
}
