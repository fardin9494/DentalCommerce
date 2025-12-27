using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Serials;

public static class TransferSerialsHelper
{
    public static async Task ReserveSerialsAsync(
        InventoryDbContext db,
        Guid stockItemId,
        Guid transferId,
        Guid transferLineId,
        Guid transferSegmentId,
        decimal qty,
        CancellationToken ct)
    {
        if (qty <= 0) return;

        var hasSerials = await db.StockItemSerials.AnyAsync(s => s.StockItemId == stockItemId, ct);
        if (!hasSerials) return;

        var qtyInt = EnsureWholeQty(qty);
        var availableSerials = await db.StockItemSerials
            .Where(s => s.StockItemId == stockItemId && s.Status == StockSerialStatus.Available)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync(ct);

        if (availableSerials.Count < qtyInt)
            throw new InvalidOperationException("Serials available do not cover the transfer quantity.");

        foreach (var serial in availableSerials.Take(qtyInt))
        {
            serial.ReserveForTransfer(transferId, transferLineId, transferSegmentId);
        }
    }

    public static async Task ReleaseReservedSerialsAsync(
        InventoryDbContext db,
        Guid transferSegmentId,
        CancellationToken ct)
    {
        var reserved = await db.StockItemSerials
            .Where(s => s.TransferSegmentId == transferSegmentId && s.Status == StockSerialStatus.Reserved)
            .ToListAsync(ct);

        if (reserved.Count == 0) return;

        var stockItemIds = reserved
            .Where(s => s.StockItemId.HasValue)
            .Select(s => s.StockItemId!.Value)
            .Distinct()
            .ToList();

        var shelvedLookup = stockItemIds.Count == 0
            ? new Dictionary<Guid, bool>()
            : await db.StockItems
                .AsNoTracking()
                .Where(si => stockItemIds.Contains(si.Id))
                .ToDictionaryAsync(si => si.Id, si => si.ShelfId != null, ct);

        foreach (var serial in reserved)
        {
            var isShelved = serial.StockItemId.HasValue &&
                            shelvedLookup.TryGetValue(serial.StockItemId.Value, out var shelved) &&
                            shelved;
            serial.ReleaseTransferReservation(isShelved);
        }
    }

    public static async Task MarkInTransitAsync(
        InventoryDbContext db,
        Guid transferSegmentId,
        CancellationToken ct)
    {
        var reserved = await db.StockItemSerials
            .Where(s => s.TransferSegmentId == transferSegmentId && s.Status == StockSerialStatus.Reserved)
            .ToListAsync(ct);

        if (reserved.Count == 0) return;

        foreach (var serial in reserved)
        {
            serial.MarkInTransit();
        }
    }

    public static async Task ReceiveSerialsAsync(
        InventoryDbContext db,
        Guid transferSegmentId,
        Guid destStockItemId,
        decimal qty,
        bool isShelved,
        CancellationToken ct)
    {
        if (qty <= 0) return;

        var hasSerials = await db.StockItemSerials.AnyAsync(s => s.TransferSegmentId == transferSegmentId, ct);
        if (!hasSerials) return;

        var qtyInt = EnsureWholeQty(qty);
        var inTransit = await db.StockItemSerials
            .Where(s => s.TransferSegmentId == transferSegmentId &&
                        (s.Status == StockSerialStatus.InTransit || s.Status == StockSerialStatus.Reserved))
            .OrderBy(s => s.SerialNumber)
            .ToListAsync(ct);

        if (inTransit.Count < qtyInt)
            throw new InvalidOperationException("Transfer serials do not cover the received quantity.");

        foreach (var serial in inTransit.Take(qtyInt))
        {
            serial.ReceiveTransfer(destStockItemId, isShelved);
        }
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized transfers require whole-number quantities.");
        return (int)truncated;
    }
}
