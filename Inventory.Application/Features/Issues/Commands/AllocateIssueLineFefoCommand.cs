using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record AllocateIssueLineFefoCommand(Guid IssueId, Guid LineId) : IRequest<IReadOnlyList<AllocationDto>>;

public sealed record AllocationDto(Guid StockItemId, decimal Qty);