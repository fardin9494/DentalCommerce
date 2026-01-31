namespace Sales.Application.Features.Orders.Models;

public sealed record OrderTimelineDto(
    Guid Id,
    Guid OrderId,
    string EventType,
    string? FromStatus,
    string? ToStatus,
    string? Message,
    string? DataJson,
    DateTime CreatedAt);
