using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

/// <summary>
/// رد کردن یک خط از رسید
/// Qty: مقدار کل رد شده (نه مقدار اضافه شده)
/// </summary>
public sealed record RejectReceiptLineCommand(
    Guid ReceiptId,
    Guid LineId,
    decimal Qty,  // مقدار کل رد شده جدید
    string? Reason = null
) : IRequest<Unit>;

