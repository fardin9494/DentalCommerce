using MediatR;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed record GetEffectiveStockItemPriceQuery(Guid StockItemId, DateTime? AtUtc = null)
    : IRequest<StockItemPriceDto>;

public sealed record StockItemPriceDto(Guid PriceId, Guid StockItemId, decimal Amount, string Currency,
    DateTime EffectiveFrom, DateTime? EffectiveTo);