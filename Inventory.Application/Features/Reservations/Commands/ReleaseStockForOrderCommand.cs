using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Reservations.Commands;

public sealed record ReleaseStockForOrderCommand(Guid OrderId) : IRequest<ReleaseStockResult>;

public sealed record ReleaseStockResult(
    Guid OrderId,
    IReadOnlyList<ReleasedStockItem> Released);

public sealed record ReleasedStockItem(
    Guid StockItemId,
    string SkuId,
    decimal Qty);

public sealed class ReleaseStockForOrderHandler : IRequestHandler<ReleaseStockForOrderCommand, ReleaseStockResult>
{
    private readonly IInventoryDbContext _db;

    public ReleaseStockForOrderHandler(IInventoryDbContext db) => _db = db;

    public async Task<ReleaseStockResult> Handle(ReleaseStockForOrderCommand req, CancellationToken ct)
    {
        if (req.OrderId == Guid.Empty) throw new ArgumentException("OrderId required.", nameof(req.OrderId));

        var reservations = await _db.StockReservations
            .Where(r => r.OrderId == req.OrderId)
            .ToListAsync(ct);

        if (reservations.Count == 0)
            return new ReleaseStockResult(req.OrderId, Array.Empty<ReleasedStockItem>());

        var stockItemIds = reservations.Select(r => r.StockItemId).Distinct().ToList();
        var stockItems = await _db.StockItems
            .Where(si => stockItemIds.Contains(si.Id))
            .ToDictionaryAsync(si => si.Id, ct);

        var released = new List<ReleasedStockItem>(reservations.Count);

        foreach (var res in reservations)
        {
            if (!stockItems.TryGetValue(res.StockItemId, out var stockItem))
                throw new InvalidOperationException($"StockItem not found for reservation {res.Id}.");

            stockItem.Increase(res.Qty);
            released.Add(new ReleasedStockItem(res.StockItemId, res.Sku, res.Qty));
            _db.StockReservations.Remove(res);
        }

        await _db.SaveChangesAsync(ct);

        return new ReleaseStockResult(req.OrderId, released);
    }
}
