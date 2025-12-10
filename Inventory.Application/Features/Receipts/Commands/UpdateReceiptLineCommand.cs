using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record UpdateReceiptLineCommand(
    Guid ReceiptId,
    Guid LineId,
    decimal? Qty = null,
    decimal? UnitCost = null,
    string? LotNumber = null,
    DateTime? ExpiryDateUtc = null
) : IRequest<Unit>;

