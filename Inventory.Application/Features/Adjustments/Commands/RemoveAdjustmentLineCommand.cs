using MediatR;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed record RemoveAdjustmentLineCommand(Guid AdjustmentId, Guid LineId) : IRequest<Unit>;

