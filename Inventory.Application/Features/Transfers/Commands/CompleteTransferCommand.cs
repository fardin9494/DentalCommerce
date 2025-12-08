using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed record CompleteTransferCommand(
    Guid TransferId,
    DateTime? WhenUtc = null
) : IRequest<Unit>;

