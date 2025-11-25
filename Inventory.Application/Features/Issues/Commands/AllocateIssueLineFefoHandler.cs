using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AllocateIssueLineFefoHandler
    : IRequestHandler<AllocateIssueLineFefoCommand, IReadOnlyList<AllocationDto>>
{
    private readonly InventoryDbContext _db;
    public AllocateIssueLineFefoHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<AllocationDto>> Handle(AllocateIssueLineFefoCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var issue = await _db.Issues
            .Include(i => i.Lines).ThenInclude(l => l.Allocations)
            .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
            ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

        var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند خروج پیدا نشد.");

        // اگر قبلاً تخصیص داشت، ابتدا آزاد و پاک کن
        if (line.Allocations.Count > 0)
        {
            foreach (var a in line.Allocations)
            {
                var si0 = await _db.StockItems.FirstAsync(si => si.Id == a.StockItemId, ct);
                si0.Release(a.Qty);
            }
            issue.ClearAllocations(line.Id);
            await _db.SaveChangesAsync(ct);
        }

        var need = line.RemainingQty;
        if (need <= 0) return line.Allocations.Select(a => new AllocationDto(a.StockItemId, a.Qty)).ToList();

        // FEFO: موجودی‌های قابل‌فروش در همین انبار
        var stockQuery =
            _db.StockItems.AsNoTracking()
            .Where(si => si.WarehouseId == issue.WarehouseId
                      && si.ProductId == line.ProductId
                      && si.VariantId == line.VariantId
                      && (si.OnHand - si.Reserved - si.Blocked) > 0)
            .Select(si => new
            {
                si.Id,
                Available = si.OnHand - si.Reserved - si.Blocked,
                si.ExpiryDate
            })
            .OrderBy(x => x.ExpiryDate.HasValue ? 0 : 1) // انقضا دارها جلوتر
            .ThenBy(x => x.ExpiryDate);

        var candidates = await stockQuery.ToListAsync(ct);
        if (candidates.Count == 0)
            throw new InvalidOperationException("موجودی کافی برای تخصیص یافت نشد.");

        var allocations = new List<AllocationDto>();

        foreach (var c in candidates)
        {
            if (need <= 0) break;
            var take = Math.Min(need, c.Available);
            if (take <= 0) continue;

            // رزرو روی آیتم
            var si = await _db.StockItems.FirstAsync(x => x.Id == c.Id, ct);
            si.Reserve(take);

            // ثبت تخصیص در خط
           issue.AddAllocation(line.Id, c.Id, take);

            allocations.Add(new AllocationDto(c.Id, take));
            need -= take;
        }

        if (need > 0)
            throw new InvalidOperationException("موجودی کافی برای تخصیص کامل خط وجود ندارد.");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return allocations;
    }
}
