using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record CancelIssueCommand(Guid IssueId) : IRequest<Unit>;