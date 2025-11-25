namespace Inventory.Application.Features.Adjustments.Commands;

using MediatR;

public sealed record CancelAdjustmentCommand(Guid AdjustmentId) : IRequest<Unit>;