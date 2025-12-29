using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Features.Receipts.Serials;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class ReceiveReceiptHandler : IRequestHandler<ReceiveReceiptCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    private readonly ITransactionRunner _tx;

    public ReceiveReceiptHandler(
        IInventoryDbContext db,
        ICatalogGateway catalogGateway,
        ITransactionRunner tx)
    {
        _db = db;
        _catalogGateway = catalogGateway;
        _tx = tx;
    }

    public async Task<Unit> Handle(ReceiveReceiptCommand req, CancellationToken ct)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await _tx.ExecuteAsync(async ct =>
                {
                    var rec = await _db.Receipts
                        .Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    var when = req.WhenUtc ?? DateTime.UtcNow;
                    rec.Receive(when);

                    foreach (var l in rec.Lines.OrderBy(x => x.LineNo))
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                                si.ProductId == l.ProductId &&
                                si.VariantId == l.VariantId &&
                                si.WarehouseId == rec.WarehouseId &&
                                si.LotNumber == l.LotNumber &&
                                si.ExpiryDate == l.ExpiryDate &&
                                si.ShelfId == null,
                            ct);

                        if (stock is null)
                        {
                            // Fetch authoritative SKU from Catalog service via ACL
                            var catalogItem = await _catalogGateway.GetCatalogItemAsync(l.ProductId, l.VariantId, ct);
                            if (catalogItem is null)
                            {
                                var errorMessage = l.VariantId.HasValue
                                    ? $"محصول با شناسه {l.ProductId} یا variant با شناسه {l.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است."
                                    : $"محصول با شناسه {l.ProductId} در کاتالوگ یافت نشد.";
                                throw new InvalidOperationException(errorMessage);
                            }

                            stock = StockItem.Create(
                                productId: l.ProductId,
                                variantId: l.VariantId,
                                warehouseId: rec.WarehouseId,
                                sku: catalogItem.Sku,
                                lotNumber: l.LotNumber,
                                expiry: l.ExpiryDate,
                                shelfId: null
                            );

                            _db.StockItems.Add(stock);

                            if (l.UnitCost.HasValue)
                            {
                                var cost = InventoryCost.Create(stock.Id, l.UnitCost.Value, "IRR");
                                _db.InventoryCosts.Add(cost);
                            }
                        }

                        await ReceiptSerialsHelper.AttachLineSerialsToStockAsync(_db, l, stock.Id, ct);

                        stock.Increase(l.Qty);
                        stock.Block(l.Qty, "Quarantine - Waiting for Approval");

                        var entry = StockLedgerEntry.Create(
                            timestampUtc: when,
                            productId: l.ProductId,
                            variantId: l.VariantId,
                            warehouseId: rec.WarehouseId,
                            lotNumber: l.LotNumber,
                            expiryDate: l.ExpiryDate,
                            deltaQty: +l.Qty,
                            type: StockMovementType.Receipt,
                            refDocType: nameof(Receipt),
                            refDocId: rec.Id,
                            unitCost: l.UnitCost,
                            note: $"Received (Quarantine) - {rec.ExternalRef}"
                        );

                        _db.StockLedger.Add(entry);
                    }

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
