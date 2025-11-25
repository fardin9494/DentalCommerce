using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

// ورودی: می‌توانی کل باقی‌مانده را بگیری یا لیستی از بخش‌ها با مقدار دریافت

public sealed record ReceiveTransferCommand(
    Guid TransferId,
    Guid SegmentId,
    decimal Qty,
    DateTime? WhenUtc = null
) : IRequest<Unit>;
public sealed record ReceiveSegmentDto(Guid SegmentId, decimal Qty);