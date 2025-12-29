using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Serials;

public static class ReceiptSerialsHelper
{
    public static async Task AttachLineSerialsToStockAsync(
        IInventoryDbContext db,
        ReceiptLine line,
        Guid stockItemId,
        CancellationToken ct)
    {
        var serials = await db.StockItemSerials
            .Where(s => s.ReceiptLineId == line.Id)
            .ToListAsync(ct);

        if (serials.Count == 0) return;

        var qtyInt = EnsureWholeQty(line.Qty);
        if (serials.Count != qtyInt)
            throw new InvalidOperationException("Serial count does not match the line quantity.");

        foreach (var serial in serials)
        {
            serial.AttachToStock(stockItemId);
        }
    }

    public static async Task SyncLineSerialStatusesAsync(
        IInventoryDbContext db,
        ReceiptLine line,
        bool isShelved,
        CancellationToken ct)
    {
        var serials = await db.StockItemSerials
            .Where(s => s.ReceiptLineId == line.Id)
            .ToListAsync(ct);

        if (serials.Count == 0) return;

        var qtyInt = EnsureWholeQty(line.Qty);
        var approvedInt = EnsureWholeQty(line.ApprovedQty);
        var rejectedInt = EnsureWholeQty(line.RejectedQty);

        if (serials.Count != qtyInt)
            throw new InvalidOperationException("Serial count does not match the line quantity.");

        var locked = serials.Where(s => s.Status is StockSerialStatus.Issued or StockSerialStatus.Reserved or StockSerialStatus.InTransit).ToList();
        var candidates = serials
            .Where(s => s.Status is not StockSerialStatus.Issued and not StockSerialStatus.Reserved and not StockSerialStatus.InTransit)
            .OrderBy(s => s.SerialNumber)
            .ToList();

        var availableSlots = qtyInt - locked.Count;
        if (approvedInt + rejectedInt > availableSlots)
            throw new InvalidOperationException("Serial status totals exceed available slots.");

        var idx = 0;
        for (var i = 0; i < rejectedInt && idx < candidates.Count; i++, idx++)
        {
            candidates[idx].MarkRejected();
        }

        for (var i = 0; i < approvedInt && idx < candidates.Count; i++, idx++)
        {
            candidates[idx].MarkAvailable(isShelved);
        }

        for (; idx < candidates.Count; idx++)
        {
            candidates[idx].MarkQuarantine();
        }
    }

    public static async Task ResolveRejectedSerialsAsync(
        IInventoryDbContext db,
        ReceiptLine line,
        bool isShelved,
        decimal approvedQty,
        decimal returnedQty,
        decimal disposedQty,
        CancellationToken ct)
    {
        var serials = await db.StockItemSerials
            .Where(s => s.ReceiptLineId == line.Id && s.Status == StockSerialStatus.Rejected)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync(ct);

        if (serials.Count == 0) return;

        var approvedInt = EnsureWholeQty(approvedQty);
        var returnedInt = EnsureWholeQty(returnedQty);
        var disposedInt = EnsureWholeQty(disposedQty);
        var total = approvedInt + returnedInt + disposedInt;

        if (total <= 0) return;
        if (serials.Count < total)
            throw new InvalidOperationException("Not enough rejected serials to resolve.");

        var idx = 0;
        for (var i = 0; i < approvedInt; i++, idx++)
        {
            serials[idx].MarkAvailable(isShelved);
        }

        for (var i = 0; i < returnedInt; i++, idx++)
        {
            serials[idx].MarkReturned();
        }

        for (var i = 0; i < disposedInt; i++, idx++)
        {
            serials[idx].MarkDisposed();
        }
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized lines require whole-number quantities.");
        return (int)truncated;
    }
}
