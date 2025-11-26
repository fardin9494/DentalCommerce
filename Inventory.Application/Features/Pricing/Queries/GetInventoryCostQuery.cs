using MediatR;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed class GetInventoryCostQuery : IRequest<InventoryCostDto>
{
    public Guid StockItemId { get; set; }
}

public sealed record InventoryCostDto(
    Guid Id,
    Guid StockItemId,
    decimal Amount,
    string Currency,
    DateTime RecordedAt
);