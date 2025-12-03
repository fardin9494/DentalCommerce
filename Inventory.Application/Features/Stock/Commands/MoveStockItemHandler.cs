using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Commands;

public sealed class MoveStockItemHandler : IRequestHandler<MoveStockItemCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public MoveStockItemHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(MoveStockItemCommand req, CancellationToken ct)
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
                    var source = await _db.StockItems.FirstOrDefaultAsync(x => x.Id == req.SourceStockItemId, ct)
                        ?? throw new InvalidOperationException("ردیف مبدا موجود نیست.");

                    if (source.ShelfId == req.TargetShelfId)
                        throw new InvalidOperationException("محل مقصد با مبدا یکسان است.");

                    if (req.Qty > source.OnHand)
                        throw new InvalidOperationException("مقدار درخواستی بیش از موجودی است.");

                    decimal movingAvailable = 0;
                    decimal movingBlocked = 0;

                    if (req.Qty <= source.Available)
                    {
                        movingAvailable = req.Qty;
                    }
                    else
                    {
                        movingAvailable = source.Available;
                        movingBlocked = req.Qty - movingAvailable;

                        if (movingBlocked > source.Blocked)
                            throw new InvalidOperationException("مقدار مسدود بیش از حد مجاز است.");
                    }

                    var dest = await _db.StockItems.FirstOrDefaultAsync(si =>
                        si.ProductId == source.ProductId &&
                        si.VariantId == source.VariantId &&
                        si.WarehouseId == source.WarehouseId &&
                        si.LotNumber == source.LotNumber &&
                        si.ExpiryDate == source.ExpiryDate &&
                        si.ShelfId == req.TargetShelfId, ct);

                    if (dest is null)
                    {
                        dest = StockItem.Create(source.ProductId, source.VariantId, source.WarehouseId, source.LotNumber, source.ExpiryDate, req.TargetShelfId);
                        _db.StockItems.Add(dest);

                        var sourceCost = await _db.InventoryCosts.OrderByDescending(c => c.RecordedAt).FirstOrDefaultAsync(c => c.StockItemId == source.Id, ct);
                        if (sourceCost is not null)
                        {
                            _db.InventoryCosts.Add(InventoryCost.Create(dest.Id, sourceCost.Amount, sourceCost.Currency));
                        }
                    }

                    if (movingAvailable > 0)
                    {
                        source.Decrease(movingAvailable);
                        dest.Increase(movingAvailable);
                    }

                    if (movingBlocked > 0)
                    {
                        string reason = source.BlockReason ?? "Moved Stock";
                        source.Unblock(movingBlocked);
                        source.Decrease(movingBlocked);

                        dest.Increase(movingBlocked);
                        dest.Block(movingBlocked, reason);
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
}