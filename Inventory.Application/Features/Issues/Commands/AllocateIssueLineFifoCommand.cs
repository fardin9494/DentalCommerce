using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record AllocateIssueLineFifoCommand(Guid IssueId, Guid LineId, Guid? PreferredWarehouseId = null) : IRequest<IReadOnlyList<AllocationDto>>;

