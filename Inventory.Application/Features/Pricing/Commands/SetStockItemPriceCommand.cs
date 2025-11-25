using MediatR;

namespace Inventory.Application.Features.Pricing.Commands;

public sealed record SetStockItemPriceCommand(
    Guid StockItemId,
    decimal Amount,
    string Currency,
    DateTime? EffectiveFrom = null,
    DateTime? EffectiveTo = null
) : IRequest<Guid>;