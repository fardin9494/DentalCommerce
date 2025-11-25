using MediatR;

namespace Inventory.Application.Features.Issues.Commands;
public sealed record CreateIssueDraftCommand(Guid WarehouseId, string? ExternalRef = null, DateTime? DocDateUtc = null) : IRequest<Guid>;