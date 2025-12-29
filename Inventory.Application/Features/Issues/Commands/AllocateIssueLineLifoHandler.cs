using Inventory.Domain.Aggregates;
using Inventory.Application.Features.Issues.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AllocateIssueLineLifoHandler : IRequestHandler<AllocateIssueLineLifoCommand, IReadOnlyList<AllocationDto>>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public AllocateIssueLineLifoHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<IReadOnlyList<AllocationDto>> Handle(AllocateIssueLineLifoCommand req, CancellationToken ct)
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
                        ?? throw new InvalidOperationException("Issue not found.");

                    var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId);
                    if (line is null) throw new InvalidOperationException("Issue line not found.");

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

                    var query = _db.StockItems
                        .Where(si => si.ProductId == line.ProductId
                                     && si.VariantId == line.VariantId
                                     && si.ShelfId != null
                                     && (si.OnHand - si.Reserved - si.Blocked) > 0);

                    if (req.PreferredWarehouseId.HasValue)
                    {
                        query = query.Where(si => si.WarehouseId == req.PreferredWarehouseId.Value);
                    }

                    var candidates = await query
                        .OrderByDescending(si => si.CreatedAt)
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
                        throw new InvalidOperationException($"Not enough available stock. Remaining: {qtyNeeded}");

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
