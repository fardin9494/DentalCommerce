using MediatR;

namespace Inventory.Application.Features.Issues.Commands;

public sealed record RemoveIssueLineCommand(Guid IssueId, Guid LineId) : IRequest<Unit>;

