using Inventory.Application.Features.Issues.Serials;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
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
    private readonly InventoryDbContext _db;
    public AllocateIssueLineSerialsHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(AllocateIssueLineSerialsCommand req, CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

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
                        ?? throw new InvalidOperationException("Ø³Ù†Ø¯ Ø®Ø±ÙˆØ¬ Ù¾ÛŒØ¯Ø§ Ù†Ø´Ø¯.");

                    if (issue.Status != IssueStatus.Draft)
                        throw new InvalidOperationException("ØªØ®ØµÛŒØµ Ø³Ø±ÛŒØ§Ù„ ÙÙ‚Ø· Ø¯Ø± ÙˆØ¶Ø¹ÛŒØª Ù¾ÛŒØ´â€ŒÙ†ÙˆÛŒØ³ Ù…Ù…Ú©Ù† Ø§Ø³Øª.");

                    var line = issue.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("Ø®Ø· Ø³Ù†Ø¯ Ø®Ø±ÙˆØ¬ Ù¾ÛŒØ¯Ø§ Ù†Ø´Ø¯.");

                    var normalized = (req.Serials ?? Array.Empty<string>())
                        .Select(s => s?.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!)
                        .ToList();

                    var distinct = normalized
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (distinct.Count == 0)
                        throw new InvalidOperationException("Ù‡ÛŒÚ† Ø³Ø±ÛŒØ§Ù„ÛŒ Ø§Ù†ØªØ®Ø§Ø¨ Ù†Ø´Ø¯Ù‡ Ø§Ø³Øª.");

                    if (distinct.Count != normalized.Count)
                        throw new InvalidOperationException("Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ø¨Ø§ÛŒØ¯ ÛŒÚ©ØªØ§ Ø¨Ø§Ø´Ù†Ø¯.");

                    var qtyInt = EnsureWholeQty(line.RequestedQty);
                    if (distinct.Count != qtyInt)
                        throw new InvalidOperationException("ØªØ¹Ø¯Ø§Ø¯ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ø¨Ø§ÛŒØ¯ Ø¨Ø±Ø§Ø¨Ø± Ù…Ù‚Ø¯Ø§Ø± Ø®Ø· Ø¨Ø§Ø´Ø¯.");

                    var serialEntities = await _db.StockItemSerials
                        .Where(s => distinct.Contains(s.SerialNumber))
                        .ToListAsync(ct);

                    if (serialEntities.Count != distinct.Count)
                        throw new InvalidOperationException("Ø¨Ø±Ø®ÛŒ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ù¾ÛŒØ¯Ø§ Ù†Ø´Ø¯Ù†Ø¯.");

                    var notAvailable = serialEntities
                        .Where(s => !(s.Status == StockSerialStatus.Available ||
                                      (s.Status == StockSerialStatus.Reserved && s.IssueLineId == line.Id)))
                        .Select(s => s.SerialNumber)
                        .ToList();

                    if (notAvailable.Count > 0)
                        throw new InvalidOperationException("Ø¨Ø±Ø®ÛŒ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ø¯Ø± ÙˆØ¶Ø¹ÛŒØª Ù‚Ø§Ø¨Ù„ ØªØ®ØµÛŒØµ Ù†ÛŒØ³ØªÙ†Ø¯.");

                    if (serialEntities.Any(s => s.StockItemId == null))
                        throw new InvalidOperationException("Ø¨Ø±Ø®ÛŒ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ø¨Ù‡ Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ù…ØªØµÙ„ Ù†ÛŒØ³ØªÙ†Ø¯.");

                    var stockItemIds = serialEntities.Select(s => s.StockItemId!.Value).Distinct().ToList();
                    var stockItems = await _db.StockItems
                        .Where(si => stockItemIds.Contains(si.Id))
                        .ToListAsync(ct);

                    if (stockItems.Count != stockItemIds.Count)
                        throw new InvalidOperationException("Ø§Ø·Ù„Ø§Ø¹Ø§Øª Ù…ÙˆØ¬ÙˆØ¯ÛŒ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ù†Ø§Ù‚Øµ Ø§Ø³Øª.");

                    var matchesProduct = stockItems.All(si =>
                        si.ProductId == line.ProductId &&
                        (line.VariantId.HasValue ? si.VariantId == line.VariantId.Value : si.VariantId == null));

                    if (!matchesProduct)
                        throw new InvalidOperationException("Ø¨Ø±Ø®ÛŒ Ø³Ø±ÛŒØ§Ù„â€ŒÙ‡Ø§ Ù…ØªØ¹Ù„Ù‚ Ø¨Ù‡ Ø§ÛŒÙ† Ù…Ø­ØµÙˆÙ„ Ù†ÛŒØ³ØªÙ†Ø¯.");

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

                    var stockLookup = stockItems.ToDictionary(si => si.Id, si => si);
                    foreach (var group in serialEntities.GroupBy(s => s.StockItemId!.Value))
                    {
                        if (stockLookup.TryGetValue(group.Key, out var stock))
                        {
                            stock.Reserve(group.Count());
                        }

                        issue.AddAllocation(line.Id, group.Key, group.Count());
                        foreach (var serial in group)
                            serial.Reserve(issue.Id, line.Id);
                    }

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                    if (attempt == maxAttempts)
                        throw new InvalidOperationException("Concurrent update detected. Please try again.", ex);
                }
            }
        });

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

