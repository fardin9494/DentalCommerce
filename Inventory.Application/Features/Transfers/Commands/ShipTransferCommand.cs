using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;
public sealed record ShipTransferCommand(Guid TransferId, DateTime? WhenUtc = null) : IRequest<Unit>;