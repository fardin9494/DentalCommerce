using MediatR;

namespace Inventory.Application.Features.Issues.Commands;

public sealed record UpdateIssueHeaderCommand(
    Guid IssueId,
    string? ExternalRef = null,
    DateTime? DocDateUtc = null
) : IRequest<Unit>;

