using MediatR;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed record UpdateAdjustmentHeaderCommand(
    Guid AdjustmentId,
    string? Note = null,
    DateTime? DocDateUtc = null
) : IRequest<Unit>;

