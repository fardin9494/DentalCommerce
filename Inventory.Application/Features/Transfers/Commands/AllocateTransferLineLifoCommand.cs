using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;

public sealed record AllocateTransferLineLifoCommand(Guid TransferId, Guid LineId) : IRequest<IReadOnlyList<TransferAllocationDto>>;

