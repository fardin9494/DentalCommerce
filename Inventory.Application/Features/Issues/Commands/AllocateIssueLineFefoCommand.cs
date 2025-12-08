using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record AllocateIssueLineFefoCommand(Guid IssueId, Guid LineId, Guid? PreferredWarehouseId = null) : IRequest<IReadOnlyList<AllocationDto>>;

public sealed record AllocationDto(Guid StockItemId, decimal Qty);