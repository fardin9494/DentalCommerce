using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

// ورودی: می‌توانی کل باقی‌مانده را بگیری یا لیستی از بخش‌ها با مقدار دریافت
public sealed record ReceiveTransferCommand(Guid TransferId, IReadOnlyList<ReceiveSegmentDto>? Segments = null, DateTime? WhenUtc = null) : IRequest<Unit>;
public sealed record ReceiveSegmentDto(Guid SegmentId, decimal Qty);