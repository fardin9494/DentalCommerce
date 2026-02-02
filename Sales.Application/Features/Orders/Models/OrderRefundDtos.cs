namespace Sales.Application.Features.Orders.Models;

public sealed record OrderRefundLineDto(
    Guid Id,
    Guid OrderLineId,
    string SkuId,
    Guid? BatchId,
    int Quantity,
    decimal UnitAmount,
    decimal LineAmount);

public sealed record OrderRefundDto(
    Guid Id,
    Guid OrderId,
    string Status,
    string Destination,
    string Currency,
    decimal Amount,
    string? Reason,
    string? Note,
    string? RequestedBy,
    DateTime RequestedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? RejectedAtUtc,
    DateTime? CompletedAtUtc,
    string? RejectionReason,
    string? WalletReference,
    IReadOnlyList<OrderRefundLineDto> Lines);
