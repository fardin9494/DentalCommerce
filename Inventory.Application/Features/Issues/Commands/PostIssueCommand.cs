using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record PostIssueCommand(Guid IssueId, DateTime? WhenUtc = null) : IRequest<Unit>;