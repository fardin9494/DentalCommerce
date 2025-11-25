namespace Inventory.Application.Features.Adjustments.Commands;

using MediatR;

public sealed record AddAdjustmentLineCommand(
    Guid AdjustmentId,
    Guid ProductId,
    Guid? VariantId,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal QtyDelta
) : IRequest<Guid>;