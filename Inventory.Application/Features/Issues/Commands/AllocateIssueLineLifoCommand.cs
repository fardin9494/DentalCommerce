using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record AllocateIssueLineLifoCommand(Guid IssueId, Guid LineId, Guid? PreferredWarehouseId = null) : IRequest<IReadOnlyList<AllocationDto>>;

