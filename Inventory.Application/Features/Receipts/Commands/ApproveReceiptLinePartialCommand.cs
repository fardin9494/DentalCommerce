using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

/// <summary>
/// تایید جزئی یک خط از رسید
/// Qty: مقدار کل تایید شده (نه مقدار اضافه شده)
/// </summary>
public sealed record ApproveReceiptLinePartialCommand(
    Guid ReceiptId,
    Guid LineId,
    decimal Qty  // مقدار کل تایید شده جدید
) : IRequest<Unit>;

