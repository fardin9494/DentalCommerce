using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record CancelReceiptCommand(
    Guid ReceiptId
) : IRequest<Unit>;