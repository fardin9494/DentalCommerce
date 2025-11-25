using Inventory.Domain.Enums;
using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record CreateReceiptDraftCommand(
    Guid WarehouseId,
    ReceiptReason Reason,              // ⬅️ اضافه شد
    string? ExternalRef = null,
    DateTime? DocDateUtc = null
) : IRequest<Guid>;