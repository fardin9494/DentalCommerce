using Inventory.Domain.Aggregates;
using Inventory.Application.Features.Issues.Serials;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AllocateIssueLineLifoHandler : IRequestHandler<AllocateIssueLineLifoCommand, IReadOnlyList<AllocationDto>>
{
    private readonly InventoryDbContext _db;
    public AllocateIssueLineLifoHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<AllocationDto>> Handle(AllocateIssueLineLifoCommand req, CancellationToken ct)
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
                        result = allocatedResult;
                        await tx.CommitAsync(ct);
                        break;
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
