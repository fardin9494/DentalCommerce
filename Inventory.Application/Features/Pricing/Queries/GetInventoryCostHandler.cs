using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed class GetInventoryCostHandler
    : IRequestHandler<GetInventoryCostQuery, InventoryCostDto>
{
    private readonly InventoryDbContext _db;
    public GetInventoryCostHandler(InventoryDbContext db) => _db = db;

    public async Task<InventoryCostDto> Handle(GetInventoryCostQuery req, CancellationToken ct)
    {
        var cost = await _db.InventoryCosts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StockItemId == req.StockItemId, ct);

        if (cost is null)
            throw new InvalidOperationException("هزینه‌ای برای این آیتم ثبت نشده است.");

        return new InventoryCostDto(
            cost.Id,
            cost.StockItemId,
            cost.Amount,
            cost.Currency,
            cost.RecordedAt
        );
    }
}