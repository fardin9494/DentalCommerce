using MediatR;

namespace Sales.Application.Features.Orders.Commands;

public sealed record RetryPaymentCommand(Guid OrderId) : IRequest<RetryPaymentResult>;

public sealed record RetryPaymentResult(
    bool Success,
    string? FailureReason = null,
    string OrderStatus = "");
