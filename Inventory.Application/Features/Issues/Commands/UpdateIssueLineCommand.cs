using MediatR;

namespace Inventory.Application.Features.Issues.Commands;

public sealed record UpdateIssueLineCommand(
    Guid IssueId,
    Guid LineId,
    decimal Qty
) : IRequest<Unit>;

