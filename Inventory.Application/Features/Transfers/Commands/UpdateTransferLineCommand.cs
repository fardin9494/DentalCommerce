using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed record UpdateTransferLineCommand(
    Guid TransferId,
    Guid LineId,
    decimal Qty
) : IRequest<Unit>;

