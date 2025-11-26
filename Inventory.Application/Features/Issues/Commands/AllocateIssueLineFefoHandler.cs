using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AllocateIssueLineFefoHandler : IRequestHandler<AllocateIssueLineFefoCommand, IReadOnlyList<AllocationDto>>
{
    private readonly InventoryDbContext _db;
    public AllocateIssueLineFefoHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<AllocationDto>> Handle(AllocateIssueLineFefoCommand req, CancellationToken ct)
    {
        // 1. بارگذاری سند خروج و خطوط آن
        var issue = await _db.Issues
            .Include(i => i.Lines)
            .ThenInclude(l => l.Allocations) // لود کردن تخصیص‌های قبلی برای محاسبه صحیح RemainingQty
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct);

        if (issue is null) throw new InvalidOperationException("سند خروج یافت نشد.");

        // استفاده از req.LineId مطابق با ریکورد شما
        var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId);
        if (line is null) throw new InvalidOperationException("خط سند مورد نظر یافت نشد.");

        // مقدار مورد نیاز برای تخصیص
        decimal qtyNeeded = line.RemainingQty;

        // لیست خروجی برای نمایش به کلاینت (که چه چیزهایی رزرو شد)
        var allocatedResult = new List<AllocationDto>();

        if (qtyNeeded <= 0)
            return allocatedResult; // قبلاً کامل تخصیص داده شده است

        // 2. استراتژی FEFO: پیدا کردن کاندیداها
        // شرط مهم: ShelfId != null (فقط از کالاهای چیده شده در قفسه بردار)
        var candidates = await _db.StockItems
            .Where(si => si.ProductId == line.ProductId
                         && si.VariantId == line.VariantId
                         && si.WarehouseId == issue.WarehouseId
                         && si.ShelfId != null
                         && (si.OnHand - si.Reserved - si.Blocked) > 0) // موجودی آزاد دارد
            .OrderBy(si => si.ExpiryDate) // اولویت با تاریخ نزدیک‌تر
            .ToListAsync(ct);

        // 3. حلقه تخصیص
        foreach (var stock in candidates)
        {
            if (qtyNeeded <= 0) break;

            decimal available = stock.Available;
            decimal toTake = Math.Min(available, qtyNeeded);

            // الف) رزرو روی موجودی کالا
            stock.Reserve(toTake);

            // ب) ثبت تخصیص در سند خروج
            issue.AddAllocation(line.Id, stock.Id, toTake);

            // ج) افزودن به لیست خروجی
            allocatedResult.Add(new AllocationDto(stock.Id, toTake));

            qtyNeeded -= toTake;
        }

        // اگر بعد از گشتن تمام قفسه‌ها هنوز کسر داشتیم
        if (qtyNeeded > 0)
            throw new InvalidOperationException($"موجودی قابل فروش کافی در قفسه‌ها یافت نشد. مقدار کسر: {qtyNeeded}");

        await _db.SaveChangesAsync(ct);

        return allocatedResult;
    }
}