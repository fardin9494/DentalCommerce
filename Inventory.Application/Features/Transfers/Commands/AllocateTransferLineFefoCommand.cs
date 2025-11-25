using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;

public sealed record AllocateTransferLineFefoCommand(Guid TransferId, Guid LineId) : IRequest<IReadOnlyList<TransferAllocationDto>>;
public sealed record TransferAllocationDto(Guid StockItemId, decimal Qty);