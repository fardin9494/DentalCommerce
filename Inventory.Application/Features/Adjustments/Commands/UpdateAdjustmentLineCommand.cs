using MediatR;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed record UpdateAdjustmentLineCommand(
    Guid AdjustmentId,
    Guid LineId,
    decimal QtyDelta
) : IRequest<Unit>;

