namespace Inventory.Application.Features.Adjustments.Commands;

using MediatR;

public sealed record PostAdjustmentCommand(Guid AdjustmentId, DateTime? WhenUtc = null) : IRequest<Unit>;