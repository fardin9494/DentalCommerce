namespace Inventory.Application.Features.Adjustments.Commands;

using Inventory.Domain.Enums;
using MediatR;

public sealed record CreateAdjustmentDraftCommand(
    Guid WarehouseId,
    AdjustmentReason Reason,
    string? Note = null,
    DateTime? DocDateUtc = null
) : IRequest<Guid>;