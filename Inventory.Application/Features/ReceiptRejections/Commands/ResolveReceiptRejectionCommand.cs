using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Features.Receipts.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.ReceiptRejections.Commands;

public sealed record ResolveReceiptRejectionCommand(
    Guid ReceiptLineId,
    decimal ApprovedQty,
    decimal ReturnedQty,
    decimal DisposedQty,
    string? Note = null
) : IRequest<Unit>;

public sealed class ResolveReceiptRejectionHandler : IRequestHandler<ResolveReceiptRejectionCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ITransactionRunner _tx;

    public ResolveReceiptRejectionHandler(IInventoryDbContext db, ITransactionRunner tx)
    {
        _db = db;
        _tx = tx;
    }

    public async Task<Unit> Handle(ResolveReceiptRejectionCommand req, CancellationToken ct)
    {
        if (req.ApprovedQty < 0 || req.ReturnedQty < 0 || req.DisposedQty < 0)
            throw new InvalidOperationException("Resolution quantities cannot be negative.");

        if (req.ApprovedQty + req.ReturnedQty + req.DisposedQty <= 0)
            throw new InvalidOperationException("At least one resolution quantity must be greater than zero.");

        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var line = await _db.ReceiptLines
                        .FirstOrDefaultAsync(l => l.Id == req.ReceiptLineId, ct)
                        ?? throw new InvalidOperationException("Receipt line not found.");

                    if (line.RejectedQty <= 0)
                        throw new InvalidOperationException("There is no rejected quantity to resolve.");

                    if (line.RejectionStatus != ReceiptRejectionStatus.None &&
                        line.RejectionStatus != ReceiptRejectionStatus.Pending)
                        throw new InvalidOperationException("This rejection has already been resolved.");

                    var receipt = await _db.Receipts
                        .FirstOrDefaultAsync(r => r.Id == line.ReceiptId, ct)
                        ?? throw new InvalidOperationException("Receipt not found.");

                    var isShelved = false;
                    if (req.ApprovedQty > 0)
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                            si.ProductId == line.ProductId &&
                            si.VariantId == line.VariantId &&
                            si.WarehouseId == receipt.WarehouseId &&
                            si.LotNumber == line.LotNumber &&
                            si.ExpiryDate == line.ExpiryDate, ct)
                            ?? throw new InvalidOperationException("Stock item for receipt line not found.");

                        isShelved = stock.ShelfId.HasValue;
                        if (stock.ShelfId.HasValue)
                        {
                            stock.Increase(req.ApprovedQty);
                        }
                        else
                        {
                            stock.Increase(req.ApprovedQty);
                            stock.Block(req.ApprovedQty, "Awaiting Shelving");
                        }

                        var note = string.IsNullOrWhiteSpace(req.Note)
                            ? $"Receipt rejection approved - {receipt.ExternalRef ?? receipt.Id.ToString()}"
                            : req.Note.Trim();

                        var ledgerEntry = StockLedgerEntry.Create(
                            timestampUtc: DateTime.UtcNow,
                            productId: line.ProductId,
                            variantId: line.VariantId,
                            warehouseId: receipt.WarehouseId,
                            lotNumber: line.LotNumber,
                            expiryDate: line.ExpiryDate,
                            deltaQty: req.ApprovedQty,
                            type: StockMovementType.AdjustmentPlus,
                            refDocType: "ReceiptRejection",
                            refDocId: line.Id,
                            unitCost: line.UnitCost,
                            note: note
                        );
                        _db.StockLedger.Add(ledgerEntry);
                    }

                    line.ResolveRejectionAmounts(req.ApprovedQty, req.ReturnedQty, req.DisposedQty, req.Note);
                    await ReceiptSerialsHelper.ResolveRejectedSerialsAsync(_db, line, isShelved, req.ApprovedQty, req.ReturnedQty, req.DisposedQty, ct);

                    await _db.SaveChangesAsync(ct);
                }, ct);
                break;
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                _db.ChangeTracker.Clear();
            }
        }

        return Unit.Value;
    }
}
