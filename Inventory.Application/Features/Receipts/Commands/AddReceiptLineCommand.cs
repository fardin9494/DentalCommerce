using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;
public sealed record AddReceiptLineCommand(
    Guid ReceiptId,
    Guid ProductId,
    Guid? VariantId,
    decimal Qty,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal? UnitCost
) : IRequest<Guid>;