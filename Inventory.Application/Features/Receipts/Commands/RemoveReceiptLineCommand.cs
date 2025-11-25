using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;
public sealed record RemoveReceiptLineCommand(Guid ReceiptId, Guid LineId) : IRequest<Unit>;