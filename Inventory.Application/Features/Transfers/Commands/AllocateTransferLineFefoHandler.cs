using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class AllocateTransferLineFefoHandler
    : IRequestHandler<AllocateTransferLineFefoCommand, IReadOnlyList<TransferAllocationDto>>
{
    private readonly InventoryDbContext _db;
    public AllocateTransferLineFefoHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<TransferAllocationDto>> Handle(AllocateTransferLineFefoCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var tr = await _db.Transfers
            .Include(t => t.Lines).ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
            ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        var line = tr.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند انتقال پیدا نشد.");

        // آزادسازی قبلی
        if (line.Segments.Count > 0)
        {
            foreach (var s in line.Segments)
            {
                var si0 = await _db.StockItems.FirstAsync(si => si.Id == s.StockItemId, ct);
                si0.Release(s.Qty); // قبلاً رزرو کرده بودیم
            }
            tr.ClearSegments(line.Id);
            await _db.SaveChangesAsync(ct);
        }

        var need = line.RemainingQty;
        if (need <= 0)
            return line.Segments.Select(s => new TransferAllocationDto(s.StockItemId, s.Qty)).ToList();

        // FEFO از انبار مبدا
        var stockQuery =
            _db.StockItems.AsNoTracking()
            .Where(si => si.WarehouseId == tr.SourceWarehouseId
                      && si.ProductId == line.ProductId
                      && si.VariantId == line.VariantId
                      && (si.OnHand - si.Reserved - si.Blocked) > 0)
            .Select(si => new
            {
                si.Id,
                Available = si.OnHand - si.Reserved - si.Blocked,
                si.ExpiryDate
            })
            .OrderBy(x => x.ExpiryDate.HasValue ? 0 : 1)
            .ThenBy(x => x.ExpiryDate);

        var candidates = await stockQuery.ToListAsync(ct);
        if (candidates.Count == 0)
            throw new InvalidOperationException("موجودی کافی برای تخصیص یافت نشد.");

        var allocations = new List<TransferAllocationDto>();

        foreach (var c in candidates)
        {
            if (need <= 0) break;
            var take = Math.Min(need, c.Available);
            if (take <= 0) continue;

            // رزرو
            var si = await _db.StockItems.FirstAsync(x => x.Id == c.Id, ct);
            si.Reserve(take);

            tr.AddSegment(line.Id, c.Id, take);
            allocations.Add(new TransferAllocationDto(c.Id, take));
            need -= take;
        }

        if (need > 0)
            throw new InvalidOperationException("موجودی کافی برای تخصیص کامل خط وجود ندارد.");

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return allocations;
    }
}
