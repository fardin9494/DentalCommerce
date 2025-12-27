using Inventory.Domain.Aggregates;
using Inventory.Application.Features.Issues.Serials;
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
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

        IReadOnlyList<AllocationDto> result = Array.Empty<AllocationDto>();

        await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
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

                    // آزاد کردن تخصیص‌های قبلی
                    foreach (var alloc in line.Allocations.ToList())
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct);
                        if (stock != null)
                        {
                            stock.Release(alloc.Qty);
                        }
                    }
                    await IssueSerialsHelper.ReleaseReservedSerialsAsync(_db, line.Id, ct);
                    issue.ClearAllocations(req.LineId);
                    await _db.SaveChangesAsync(ct);

                    // مقدار مورد نیاز برای تخصیص (حالا باید کل RequestedQty باشد)
                    decimal qtyNeeded = line.RequestedQty;

                    // لیست خروجی برای نمایش به کلاینت (که چه چیزهایی رزرو شد)
                    var allocatedResult = new List<AllocationDto>();

                    if (qtyNeeded <= 0)
                    {
                        result = allocatedResult;
                        await tx.CommitAsync(ct);
                        break; // قبلاً کامل تخصیص داده شده است
                    }

                    // 2. استراتژی FEFO: پیدا کردن کاندیداها
                    // اگر PreferredWarehouseId مشخص شده باشد، فقط از آن انبار استفاده می‌کنیم
                    // در غیر اینصورت از تمام انبارها جستجو می‌کنیم
                    // فقط کالاهای Available قابل تخصیص هستند (در قفسه و آزاد)
                    var query = _db.StockItems
                        .Where(si => si.ProductId == line.ProductId
                                     && si.VariantId == line.VariantId
                                     && si.ShelfId != null  // باید در قفسه باشد
                                     && si.Blocked == 0     // نباید Blocked باشد
                                     && (si.OnHand - si.Reserved - si.Blocked) > 0); // موجودی آزاد دارد

                    // اگر PreferredWarehouseId مشخص شده باشد، فقط از آن انبار استفاده می‌کنیم
                    if (req.PreferredWarehouseId.HasValue)
                    {
                        query = query.Where(si => si.WarehouseId == req.PreferredWarehouseId.Value);
                    }
                    // در غیر اینصورت از تمام انبارها جستجو می‌کنیم (بدون فیلتر WarehouseId)

                    var candidates = await query
                        .OrderBy(si => si.ExpiryDate.HasValue ? 0 : 1) // اولویت با مواردی که تاریخ انقضا دارند
                        .ThenBy(si => si.ExpiryDate) // سپس بر اساس تاریخ انقضا (نزدیک‌تر اول)
                        .ToListAsync(ct);

                    // 3. حلقه تخصیص
                    foreach (var stock in candidates)
                    {
                        if (qtyNeeded <= 0) break;

                        decimal available = stock.Available;
                        decimal toTake = Math.Min(available, qtyNeeded);

                        // الف) رزرو روی موجودی کالا
                        stock.Reserve(toTake);
                        await IssueSerialsHelper.ReserveSerialsAsync(_db, stock.Id, issue.Id, line.Id, toTake, ct);

                        // ب) ثبت تخصیص در سند خروج
                        var alloc = issue.AddAllocation(line.Id, stock.Id, toTake);
                        _db.Entry(alloc).State = EntityState.Added;

                        // ج) افزودن به لیست خروجی
                        allocatedResult.Add(new AllocationDto(stock.Id, toTake));

                        qtyNeeded -= toTake;
                    }

                    // اگر بعد از گشتن تمام قفسه‌ها هنوز کسر داشتیم
                    if (qtyNeeded > 0)
                        throw new InvalidOperationException($"موجودی قابل فروش کافی در قفسه‌ها یافت نشد. مقدار کسر: {qtyNeeded}");

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    result = allocatedResult;
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }
        });

        return result;
    }
}


