using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Pricing.Commands;

// ابتدا مطمئن شوید Command شما هم فیلدهای تاریخ را حذف کرده باشد

public sealed class SetInventoryCostHandler : IRequestHandler<SetInventoryCostCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public SetInventoryCostHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(SetInventoryCostCommand req, CancellationToken ct)
    {
        // 1. بررسی وجود StockItem
        var stockItemExists = await _db.StockItems.AnyAsync(x => x.Id == req.StockItemId, ct);
        if (!stockItemExists) throw new InvalidOperationException("آیتم انبار (StockItem) یافت نشد.");

        // 2. جستجوی هزینه فعلی (Cost)
        // چون هر StockItem (که نماینده یک بچ خاص است) فقط یک هزینه خرید دارد، نیازی به تاریخ نیست.
        var existingCost = await _db.InventoryCosts
            .FirstOrDefaultAsync(c => c.StockItemId == req.StockItemId, ct);

        if (existingCost is not null)
        {
            // سناریوی اصلاح: اگر قبلاً ثبت شده، مبلغ را اصلاح می‌کنیم
            // (مثلاً انباردار اشتباه وارد کرده بود)
            existingCost.CorrectCost(req.Amount);

            // اگر نیاز به تغییر ارز هم دارید، باید متدش را در دامین اضافه کنید یا اینجا دستی ست کنید
            // existingCost.UpdateCurrency(req.Currency); 

            await _db.SaveChangesAsync(ct);
            return existingCost.Id;
        }
        else
        {
            // سناریوی ایجاد: اولین بار است که برای این بچ قیمت می‌گذاریم
            var newCost = InventoryCost.Create(req.StockItemId, req.Amount, req.Currency);
            _db.InventoryCosts.Add(newCost);

            await _db.SaveChangesAsync(ct);
            return newCost.Id;
        }
    }
}