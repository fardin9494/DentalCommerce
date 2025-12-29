using Inventory.Domain.Aggregates;
using Inventory.Application.Features.Issues.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AllocateIssueLineFifoHandler : IRequestHandler<AllocateIssueLineFifoCommand, IReadOnlyList<AllocationDto>>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public AllocateIssueLineFifoHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<IReadOnlyList<AllocationDto>> Handle(AllocateIssueLineFifoCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        IReadOnlyList<AllocationDto> result = Array.Empty<AllocationDto>();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                result = await _tx.ExecuteAsync(async ct =>
                {
                    var issue = await _db.Issues
                        .Include(i => i.Lines)
                        .ThenInclude(l => l.Allocations)
                        .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                        ?? throw new InvalidOperationException("سند خروج یافت نشد.");

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

                    decimal qtyNeeded = line.RequestedQty;
                    var allocatedResult = new List<AllocationDto>();

                    if (qtyNeeded <= 0)
                    {
                        return allocatedResult;
                    }

                    // استراتژی FIFO: بر اساس CreatedAt (قدیمی‌ترین اول)
                    // اگر PreferredWarehouseId مشخص شده باشد، فقط از آن انبار استفاده می‌کنیم
                    // در غیر اینصورت از تمام انبارها جستجو می‌کنیم
                    var query = _db.StockItems
                        .Where(si => si.ProductId == line.ProductId
                                     && si.VariantId == line.VariantId
                                     && si.ShelfId != null
                                     && (si.OnHand - si.Reserved - si.Blocked) > 0);

                    // اگر PreferredWarehouseId مشخص شده باشد، فقط از آن انبار استفاده می‌کنیم
                    if (req.PreferredWarehouseId.HasValue)
                    {
                        query = query.Where(si => si.WarehouseId == req.PreferredWarehouseId.Value);
                    }
                    // در غیر اینصورت از تمام انبارها جستجو می‌کنیم (بدون فیلتر WarehouseId)

                    var candidates = await query
                        .OrderBy(si => si.CreatedAt) // FIFO: قدیمی‌ترین اول
                        .ToListAsync(ct);

                    foreach (var stock in candidates)
                    {
                        if (qtyNeeded <= 0) break;

                        decimal available = stock.Available;
                        decimal toTake = Math.Min(available, qtyNeeded);

                        stock.Reserve(toTake);
                        await IssueSerialsHelper.ReserveSerialsAsync(_db, stock.Id, issue.Id, line.Id, toTake, ct);
                        var alloc = issue.AddAllocation(line.Id, stock.Id, toTake);
                        _db.Entry(alloc).State = EntityState.Added;

                        allocatedResult.Add(new AllocationDto(stock.Id, toTake));
                        qtyNeeded -= toTake;
                    }

                    if (qtyNeeded > 0)
                        throw new InvalidOperationException($"موجودی قابل فروش کافی در قفسه‌ها یافت نشد. مقدار کسر: {qtyNeeded}");

                    await _db.SaveChangesAsync(ct);
                    return allocatedResult;
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return result;
    }
}




