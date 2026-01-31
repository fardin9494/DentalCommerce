using Inventory.Application.Abstractions;
using Inventory.Domain.Aggregates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.Commands;

public sealed class ReserveStockForOrderHandler : IRequestHandler<ReserveStockForOrderCommand, ReserveStockResult>
{
    private readonly IInventoryDbContext _db;

    public ReserveStockForOrderHandler(IInventoryDbContext db) => _db = db;

    public async Task<ReserveStockResult> Handle(ReserveStockForOrderCommand req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));
        if (req.Lines.Count == 0) throw new ArgumentException("Lines required.", nameof(req.Lines));

        var reserved = new List<ReservedStockItem>();

        foreach (var line in req.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.SkuId)) throw new ArgumentException("SkuId required.");
            if (line.Qty <= 0) throw new ArgumentOutOfRangeException(nameof(line.Qty));

            var sku = line.SkuId.Trim();
            var remaining = line.Qty;

            var items = await _db.StockItems
                .Where(si => si.Sku.ToLower() == sku.ToLower())
                .OrderBy(si => si.ExpiryDate == null)
                .ThenBy(si => si.ExpiryDate)
                .ThenBy(si => si.CreatedAt)
                .ToListAsync(ct);

            foreach (var si in items)
            {
                var available = si.OnHand - si.Reserved - si.Blocked;
                if (available <= 0) continue;
                var take = Math.Min(available, remaining);

                si.Decrease(take);
                _db.StockReservations.Add(StockReservation.Create(req.OrderId, si.Id, si.Sku, take));
                reserved.Add(new ReservedStockItem(si.Id, si.Sku, take));

                remaining -= take;
                if (remaining <= 0) break;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"موجودی کافی برای SKU {sku} وجود ندارد.");
        }

        await _db.SaveChangesAsync(ct);

        return new ReserveStockResult(req.OrderId, reserved);
    }
}
