using Inventory.Application.Features.Issues.Serials;
using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed record AllocateIssueLineSerialsCommand(
    Guid IssueId,
    Guid LineId,
    IReadOnlyList<string> Serials
) : IRequest<Unit>;

public sealed class AllocateIssueLineSerialsHandler : IRequestHandler<AllocateIssueLineSerialsCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public AllocateIssueLineSerialsHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(AllocateIssueLineSerialsCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var issue = await _db.Issues
                        .Include(i => i.Lines)
                        .ThenInclude(l => l.Allocations)
                        .FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                        ?? throw new InvalidOperationException("Issue not found.");

                    if (issue.Status != IssueStatus.Draft)
                        throw new InvalidOperationException("Serial allocation is only allowed for draft issues.");

                    var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("Issue line not found.");

                    var normalized = (req.Serials ?? Array.Empty<string>())
                        .Select(s => s?.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!)
                        .ToList();

                    var distinct = normalized
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (distinct.Count == 0)
                        throw new InvalidOperationException("No serials selected.");

                    if (distinct.Count != normalized.Count)
                        throw new InvalidOperationException("Serials must be unique.");

                    var qtyInt = EnsureWholeQty(line.RequestedQty);
                    if (distinct.Count != qtyInt)
                        throw new InvalidOperationException("Serial count must match the requested quantity.");

                    var serialEntities = await _db.StockItemSerials
                        .Where(s => distinct.Contains(s.SerialNumber))
                        .ToListAsync(ct);

                    if (serialEntities.Count != distinct.Count)
                        throw new InvalidOperationException("Some serials could not be found.");

                    var notAvailable = serialEntities
                        .Where(s => !(s.Status == StockSerialStatus.Available ||
                                      (s.Status == StockSerialStatus.Reserved && s.IssueLineId == line.Id)))
                        .Select(s => s.SerialNumber)
                        .ToList();

                    if (notAvailable.Count > 0)
                        throw new InvalidOperationException("Some serials are not available for allocation.");

                    if (serialEntities.Any(s => s.StockItemId == null))
                        throw new InvalidOperationException("Some serials are not linked to stock items.");

                    var stockItemIds = serialEntities.Select(s => s.StockItemId!.Value).Distinct().ToList();
                    var stockItems = await _db.StockItems
                        .Where(si => stockItemIds.Contains(si.Id))
                        .ToListAsync(ct);

                    if (stockItems.Count != stockItemIds.Count)
                        throw new InvalidOperationException("Stock item data is incomplete for selected serials.");

                    var matchesProduct = stockItems.All(si =>
                        si.ProductId == line.ProductId &&
                        (line.VariantId.HasValue ? si.VariantId == line.VariantId.Value : si.VariantId == null));

                    if (!matchesProduct)
                        throw new InvalidOperationException("Some serials do not belong to this product.");

                    foreach (var alloc in line.Allocations.ToList())
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == alloc.StockItemId, ct);
                        if (stock != null)
                        {
                            stock.Release(alloc.Qty);
                        }
                    }

                    await IssueSerialsHelper.ReleaseReservedSerialsAsync(_db, line.Id, ct);
                    issue.ClearAllocations(line.Id);
                    await _db.SaveChangesAsync(ct);

                    var stockLookup = stockItems.ToDictionary(si => si.Id, si => si);
                    foreach (var group in serialEntities.GroupBy(s => s.StockItemId!.Value))
                    {
                        if (stockLookup.TryGetValue(group.Key, out var stock))
                        {
                            stock.Reserve(group.Count());
                        }

                        var alloc = issue.AddAllocation(line.Id, group.Key, group.Count());
                        _db.Entry(alloc).State = EntityState.Added;

                        foreach (var serial in group)
                        {
                            serial.Reserve(issue.Id, line.Id);
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _db.ChangeTracker.Clear();
                throw new InvalidOperationException("Concurrent update detected. Please try again.", ex);
            }
        }

        return Unit.Value;
    }
    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("مقدار خط باید عدد صحیح باشد.");
        return (int)truncated;
    }
}


