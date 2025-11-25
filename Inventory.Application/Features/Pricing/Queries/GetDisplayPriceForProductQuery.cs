using MediatR;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed record GetDisplayPriceForProductQuery(
    Guid ProductId,
    Guid? VariantId = null,
    Guid? WarehouseId = null,
    DateTime? AtUtc = null
) : IRequest<DisplayPriceDto>;

public sealed record DisplayPriceDto(decimal Amount, string Currency, Guid StockItemId);