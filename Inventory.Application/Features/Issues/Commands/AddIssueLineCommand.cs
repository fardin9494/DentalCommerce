using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record AddIssueLineCommand(Guid IssueId, Guid ProductId, Guid? VariantId, decimal Qty) : IRequest<Guid>;