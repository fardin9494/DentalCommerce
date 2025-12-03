using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record UpdateReceiptLineCommand(
    Guid ReceiptId,
    Guid LineId,
    decimal? Qty = null,
    decimal? UnitCost = null
) : IRequest<Unit>;

