using MediatR;

namespace Inventory.Application.Features.Pricing.Queries;

/// <summary>
/// درخواست برای دریافت هزینه تمام شده (Cost) کالاهای موجود.
/// معمولاً ارزان‌ترین بچ موجود را برمی‌گرداند.
/// </summary>
public sealed record GetAvailableStockCostQuery(
    Guid ProductId,
    Guid? VariantId = null,
    Guid? WarehouseId = null
) : IRequest<StockCostDto>;

public sealed record StockCostDto(
    decimal Amount,
    string Currency,
    Guid StockItemId
);