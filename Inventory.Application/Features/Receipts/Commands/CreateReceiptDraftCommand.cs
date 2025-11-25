using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;
public sealed record CreateReceiptDraftCommand(Guid WarehouseId, string? ExternalRef = null, DateTime? DocDateUtc = null) : IRequest<Guid>;