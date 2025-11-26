using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Queries;

public sealed class GetAvailableStockCostHandler
    : IRequestHandler<GetAvailableStockCostQuery, StockCostDto>
{
    private readonly InventoryDbContext _db;

    public GetAvailableStockCostHandler(InventoryDbContext db) => _db = db;

    public async Task<StockCostDto> Handle(GetAvailableStockCostQuery req, CancellationToken ct)
    {
        var q =
            from si in _db.StockItems.AsNoTracking()
            // 1. فیلتر کردن کالاهای موجود
            where si.ProductId == req.ProductId
                  && (req.VariantId == null || si.VariantId == req.VariantId)
                  && (req.WarehouseId == null || si.WarehouseId == req.WarehouseId)
                  // موجودی در دسترس (Available = OnHand - Reserved - Blocked)
                  && (si.OnHand - si.Reserved - si.Blocked) > 0

            // 2. جوین با جدول هزینه (InventoryCost)
            join ic in _db.InventoryCosts.AsNoTracking()
                on si.Id equals ic.StockItemId

            // 3. انتخاب خروجی
            select new { si.Id, ic.Amount, ic.Currency };

        // استراتژی: انتخاب کمترین قیمت خرید
        var best = await q.OrderBy(x => x.Amount).FirstOrDefaultAsync(ct);

        if (best is null)
            throw new InvalidOperationException("هیچ موجودی قیمت‌گذاری شده‌ای یافت نشد (No available stock cost found).");

        return new StockCostDto(best.Amount, best.Currency, best.Id);
    }
}