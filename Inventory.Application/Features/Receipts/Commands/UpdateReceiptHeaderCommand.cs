using MediatR;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record UpdateReceiptHeaderCommand(
    Guid ReceiptId,
    string? ExternalRef = null,
    DateTime? DocDateUtc = null
) : IRequest<Unit>;

