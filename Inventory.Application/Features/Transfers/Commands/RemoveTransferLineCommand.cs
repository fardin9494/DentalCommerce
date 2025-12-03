using MediatR;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed record RemoveTransferLineCommand(Guid TransferId, Guid LineId) : IRequest<Unit>;

