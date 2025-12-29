using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Serials;

public static class IssueSerialsHelper
{
    public static async Task ReserveSerialsAsync(
        IInventoryDbContext db,
        Guid stockItemId,
        Guid issueId,
        Guid issueLineId,
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
            throw new InvalidOperationException("Serials available do not cover the requested issue quantity.");

        foreach (var serial in availableSerials.Take(qtyInt))
        {
            serial.Reserve(issueId, issueLineId);
        }
    }

    public static async Task ReleaseReservedSerialsAsync(
        IInventoryDbContext db,
        Guid issueLineId,
        CancellationToken ct)
    {
        var reserved = await db.StockItemSerials
            .Where(s => s.IssueLineId == issueLineId && s.Status == StockSerialStatus.Reserved)
            .ToListAsync(ct);

        if (reserved.Count == 0) return;

        foreach (var serial in reserved)
        {
            serial.ReleaseReservation(true);
        }
    }

    public static async Task MarkIssuedAsync(
        IInventoryDbContext db,
        Guid stockItemId,
        Guid issueId,
        Guid issueLineId,
        decimal qty,
        DateTime whenUtc,
        CancellationToken ct)
    {
        if (qty <= 0) return;

        var qtyInt = EnsureWholeQty(qty);

        var reserved = await db.StockItemSerials
            .Where(s => s.StockItemId == stockItemId &&
                        s.Status == StockSerialStatus.Reserved &&
                        s.IssueLineId == issueLineId)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync(ct);

        if (reserved.Count > 0)
        {
            if (reserved.Count < qtyInt)
                throw new InvalidOperationException("Reserved serials do not cover the issued quantity.");

            foreach (var serial in reserved.Take(qtyInt))
            {
                serial.MarkIssued(issueId, issueLineId, whenUtc);
            }

            return;
        }

        var hasSerials = await db.StockItemSerials.AnyAsync(s => s.StockItemId == stockItemId, ct);
        if (!hasSerials) return;

        var available = await db.StockItemSerials
            .Where(s => s.StockItemId == stockItemId && s.Status == StockSerialStatus.Available)
            .OrderBy(s => s.SerialNumber)
            .ToListAsync(ct);

        if (available.Count < qtyInt)
            throw new InvalidOperationException("Available serials do not cover the issued quantity.");

        foreach (var serial in available.Take(qtyInt))
        {
            serial.MarkIssued(issueId, issueLineId, whenUtc);
        }
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized issues require whole-number quantities.");
        return (int)truncated;
    }
}
