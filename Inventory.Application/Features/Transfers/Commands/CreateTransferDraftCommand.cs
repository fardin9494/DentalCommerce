using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;
public sealed record CreateTransferDraftCommand(Guid SourceWarehouseId, Guid DestinationWarehouseId, string? ExternalRef = null, DateTime? DocDateUtc = null) : IRequest<Guid>;