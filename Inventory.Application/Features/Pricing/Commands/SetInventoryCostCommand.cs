using MediatR;

namespace Inventory.Application.Features.Pricing.Commands;

public sealed class SetInventoryCostCommand : IRequest<Guid>
{
    public Guid StockItemId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "IRR";
}