using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;

public sealed record AllocateTransferLineFifoCommand(Guid TransferId, Guid LineId) : IRequest<IReadOnlyList<TransferAllocationDto>>;

