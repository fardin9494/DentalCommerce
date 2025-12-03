using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed record UpdateTransferHeaderCommand(
    Guid TransferId,
    string? ExternalRef = null,
    DateTime? DocDateUtc = null
) : IRequest<Unit>;

