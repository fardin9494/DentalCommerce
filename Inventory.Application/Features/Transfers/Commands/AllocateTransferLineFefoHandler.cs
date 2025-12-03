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
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

        IReadOnlyList<TransferAllocationDto> result = Array.Empty<TransferAllocationDto>();

        await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var tr = await _db.Transfers
                        .Include(t => t.Lines).ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("انتقال پیدا نشد.");

                    var line = tr.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("خط انتقال پیدا نشد.");

                    if (line.Segments.Count > 0)
                    {
                        foreach (var s in line.Segments)
                        {
                            var si0 = await _db.StockItems.FirstAsync(si => si.Id == s.StockItemId, ct);
                            si0.Release(s.Qty);
                        }
                        tr.ClearSegments(line.Id);
                        await _db.SaveChangesAsync(ct);
                    }

                    var need = line.RemainingQty;
                    if (need <= 0)
                    {
                        result = line.Segments.Select(s => new TransferAllocationDto(s.StockItemId, s.Qty)).ToList();
                        await tx.CommitAsync(ct);
                        break;
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
                        allocations.Add(new TransferAllocationDto(c.Id, take));
                        need -= take;
                    }

                    if (need > 0)
                        throw new InvalidOperationException("موجودی کافی برای تخصیص باقی‌مانده نیست.");

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    result = allocations;
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