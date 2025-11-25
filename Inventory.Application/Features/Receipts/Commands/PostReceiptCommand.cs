using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;
public sealed record PostReceiptCommand(Guid ReceiptId, DateTime? WhenUtc = null) : IRequest<Unit>;