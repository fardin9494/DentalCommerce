using MediatR;
namespace Inventory.Application.Features.Transfers.Commands;
public sealed record CancelTransferCommand(Guid TransferId) : IRequest<Unit>;