using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Features.Receipts.Serials;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class RejectReceiptLineHandler : IRequestHandler<RejectReceiptLineCommand, Unit>
{
    private readonly InventoryDbContext _db;

    public RejectReceiptLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RejectReceiptLineCommand req, CancellationToken ct)
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
                    var rec = await _db.Receipts
                        .Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                              ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    if (rec.Status != ReceiptStatus.Received)
                        throw new InvalidOperationException("فقط رسیدهای دریافت شده قابل رد هستند.");

                    var line = rec.FindLine(req.LineId)
                               ?? throw new InvalidOperationException("خط رسید پیدا نشد.");

                    if (line.RejectionStatus is ReceiptRejectionStatus.ApprovedToStock
                        or ReceiptRejectionStatus.Returned
                        or ReceiptRejectionStatus.Disposed
                        or ReceiptRejectionStatus.Mixed)
                        throw new InvalidOperationException("برای این خط، رسیدگی به اقلام رد شده انجام شده است و امکان تغییر ندارد.");

                    var oldRejectedQty = line.RejectedQty;
                    var deltaQty = req.Qty - oldRejectedQty;

                    line.Reject(req.Qty, req.Reason);

                    var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                        si.ProductId == line.ProductId &&
                        si.VariantId == line.VariantId &&
                        si.WarehouseId == rec.WarehouseId &&
                        si.LotNumber == line.LotNumber &&
                        si.ExpiryDate == line.ExpiryDate, ct)
                                ?? throw new InvalidOperationException($"موجودی مربوط به خط {line.LineNo} یافت نشد.");


                    var newOnHand = line.Qty - line.RejectedQty;
                    var remainingQty = line.Qty - line.ApprovedQty - line.RejectedQty;
                    var newBlocked = line.ApprovedQty + remainingQty;
                    
                    var onHandDelta = newOnHand - stock.OnHand;
                    
                    if (!stock.ShelfId.HasValue)
                    {
                        if (onHandDelta < 0)
                        {
                            stock.DecreaseFromBlocked(-onHandDelta);
                        }
                        else if (onHandDelta > 0)
                        {
                            stock.Increase(onHandDelta);
                        }
                        
                        var blockReason = line.ApprovedQty > 0 
                            ? "Awaiting Shelving" 
                            : "Quarantine - Waiting for Approval";
                        stock.SetBlocked(stock.OnHand, blockReason);
                    }
                    else
                    {
                        if (onHandDelta < 0)
                        {
                            stock.DecreaseFromBlocked(-onHandDelta);
                        }
                        else if (onHandDelta > 0)
                        {
                            stock.Increase(onHandDelta);
                        }
                        
                        var blockedDelta = newBlocked - stock.Blocked;
                        if (blockedDelta < 0)
                        {
                            stock.Unblock(-blockedDelta);
                        }
                    }
                    
                    if (deltaQty != 0)
                    {
                        StockLedgerEntry ledgerEntry;
                        if (deltaQty > 0)
                        {
                            ledgerEntry = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: line.ProductId,
                                variantId: line.VariantId,
                                warehouseId: rec.WarehouseId,
                                lotNumber: line.LotNumber,
                                expiryDate: line.ExpiryDate,
                                deltaQty: -deltaQty,
                                type: StockMovementType.AdjustmentMinus,
                                refDocType: nameof(Receipt),
                                refDocId: rec.Id,
                                unitCost: line.UnitCost,
                                note: $"Rejected from Receipt - {req.Reason ?? "بدون دلیل"}"
                            );
                        }
                        else
                        {
                            ledgerEntry = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: line.ProductId,
                                variantId: line.VariantId,
                                warehouseId: rec.WarehouseId,
                                lotNumber: line.LotNumber,
                                expiryDate: line.ExpiryDate,
                                deltaQty: -deltaQty,
                                type: StockMovementType.AdjustmentPlus,
                                refDocType: nameof(Receipt),
                                refDocId: rec.Id,
                                unitCost: line.UnitCost,
                                note: $"Re-rejected from Receipt - Returned to Quarantine"
                            );
                        }
                        _db.StockLedger.Add(ledgerEntry);
                    }


                    await ReceiptSerialsHelper.SyncLineSerialStatusesAsync(_db, line, stock.ShelfId.HasValue, ct);
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
