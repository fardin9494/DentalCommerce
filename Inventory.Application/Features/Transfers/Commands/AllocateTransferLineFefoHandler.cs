using Inventory.Application.Features.Transfers.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class AllocateTransferLineFefoHandler
    : IRequestHandler<AllocateTransferLineFefoCommand, IReadOnlyList<TransferAllocationDto>>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public AllocateTransferLineFefoHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<IReadOnlyList<TransferAllocationDto>> Handle(AllocateTransferLineFefoCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        IReadOnlyList<TransferAllocationDto> result = Array.Empty<TransferAllocationDto>();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                result = await _tx.ExecuteAsync(async ct =>
                {
                    var tr = await _db.Transfers
                        .AsSplitQuery()
                        .Include(t => t.Lines).ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("انتقال پیدا نشد.");

                    var line = tr.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("خط انتقال پیدا نشد.");

                    // Save existing segments before clearing (for return value if already fully allocated)
                    var existingSegments = line.Segments.Select(s => new TransferAllocationDto(s.StockItemId, s.Qty)).ToList();
                    
                    // Calculate need before clearing
                    var need = line.RemainingQty;
                    
                    if (need <= 0)
                    {
                        // Already fully allocated, return existing segments
                        return existingSegments;
                    }

                    // Clear existing segments and release reservations
                    if (line.Segments.Count > 0)
                    {
                        foreach (var s in line.Segments)
                        {
                            var si0 = await _db.StockItems.FirstAsync(si => si.Id == s.StockItemId, ct);
                            si0.Release(s.Qty);
                            await TransferSerialsHelper.ReleaseReservedSerialsAsync(_db, s.Id, ct);
                        }
                        tr.ClearSegments(line.Id);
                        await _db.SaveChangesAsync(ct);
                    }

                    var candidates = await _db.StockItems.AsNoTracking()
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
                        .ThenBy(x => x.ExpiryDate)
                        .ToListAsync(ct);

                    if (candidates.Count == 0)
                        throw new InvalidOperationException("موجودی کافی برای تخصیص وجود ندارد.");

                    var allocations = new List<TransferAllocationDto>();

                    foreach (var c in candidates)
                    {
                        if (need <= 0) break;
                        var take = Math.Min(need, c.Available);
                        if (take <= 0) continue;

                        var si = await _db.StockItems.FirstAsync(x => x.Id == c.Id, ct);
                        si.Reserve(take);

                        var segment = tr.AddSegment(line.Id, c.Id, take);
                        _db.Entry(segment).State = EntityState.Added;
                        await TransferSerialsHelper.ReserveSerialsAsync(_db, c.Id, tr.Id, line.Id, segment.Id, take, ct);
                        allocations.Add(new TransferAllocationDto(c.Id, take));
                        need -= take;
                    }

                    if (need > 0)
                        throw new InvalidOperationException("موجودی کافی برای تخصیص باقی‌مانده نیست.");

                    await _db.SaveChangesAsync(ct);
                    return allocations;
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
