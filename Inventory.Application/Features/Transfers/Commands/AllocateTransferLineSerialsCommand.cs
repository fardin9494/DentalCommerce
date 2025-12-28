using Inventory.Application.Features.Transfers.Serials;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed record AllocateTransferLineSerialsCommand(
    Guid TransferId,
    Guid LineId,
    IReadOnlyList<string> Serials
) : IRequest<Unit>;

public sealed class AllocateTransferLineSerialsHandler
    : IRequestHandler<AllocateTransferLineSerialsCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public AllocateTransferLineSerialsHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(AllocateTransferLineSerialsCommand req, CancellationToken ct)
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
                    var transfer = await _db.Transfers
                        .Include(t => t.Lines)
                        .ThenInclude(l => l.Segments)
                        .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                        ?? throw new InvalidOperationException("Transfer not found.");

                    if (transfer.Status != TransferStatus.Draft)
                        throw new InvalidOperationException("Serial allocation is only allowed in draft transfers.");

                    var line = transfer.Lines.FirstOrDefault(l => l.Id == req.LineId)
                               ?? throw new InvalidOperationException("Transfer line not found.");

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
                                      (s.Status == StockSerialStatus.Reserved && s.TransferLineId == line.Id)))
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

                    if (stockItems.Any(si => si.WarehouseId != transfer.SourceWarehouseId))
                        throw new InvalidOperationException("Some serials are not in the source warehouse.");

                    foreach (var seg in line.Segments.ToList())
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == seg.StockItemId, ct);
                        if (stock != null)
                        {
                            stock.Release(seg.Qty);
                        }

                        await TransferSerialsHelper.ReleaseReservedSerialsAsync(_db, seg.Id, ct);
                    }

                    transfer.ClearSegments(line.Id);

                    var stockLookup = stockItems.ToDictionary(si => si.Id, si => si);
                    foreach (var group in serialEntities.GroupBy(s => s.StockItemId!.Value))
                    {
                        if (stockLookup.TryGetValue(group.Key, out var stock))
                        {
                            stock.Reserve(group.Count());
                        }

                        var segment = transfer.AddSegment(line.Id, group.Key, group.Count());
                        _db.Entry(segment).State = EntityState.Added;

                        foreach (var serial in group)
                        {
                            serial.ReserveForTransfer(transfer.Id, line.Id, segment.Id);
                        }
                    }

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }
        });

        return Unit.Value;
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized transfers require whole-number quantities.");
        return (int)truncated;
    }
}
