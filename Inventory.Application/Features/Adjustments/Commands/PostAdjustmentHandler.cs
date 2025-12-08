using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class PostAdjustmentHandler : IRequestHandler<PostAdjustmentCommand, Unit>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;

    public PostAdjustmentHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<Unit> Handle(PostAdjustmentCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments
            .Include(a => a.Lines)
            .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct);

        if (adj is null)
            throw new InvalidOperationException("سند اصلاح موجودی یافت نشد.");

        if (adj.Status != AdjustmentStatus.Draft)
            throw new InvalidOperationException("فقط سند پیش‌نویس قابل ثبت است.");

        if (adj.Lines.Count == 0)
            throw new InvalidOperationException("سند بدون آیتم قابل ثبت نیست.");

        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            const int maxAttempts = 5;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    foreach (var l in adj.Lines)
                    {
                        try
                        {
                            // پیدا/ایجاد StockItem
                            var si = await _db.StockItems.FirstOrDefaultAsync(x =>
                                    x.ProductId == l.ProductId &&
                                    x.VariantId == l.VariantId &&
                                    x.WarehouseId == adj.WarehouseId &&
                                    x.LotNumber == l.LotNumber &&
                                    x.ExpiryDate == l.ExpiryDate,
                                ct);

                            if (si is null)
                            {
                                // اگر QtyDelta منفی است، نمی‌توانیم StockItem جدید ایجاد کنیم
                                if (l.QtyDelta < 0)
                                {
                                    string productName = l.ProductId.ToString();
                                    string variantName = l.VariantId?.ToString() ?? "بدون واریانت";

                                    try
                                    {
                                        var productInfo =
                                            await _catalogGateway.GetCatalogItemAsync(l.ProductId, l.VariantId, ct);
                                        productName = productInfo?.Name ?? productName;
                                        if (l.VariantId.HasValue)
                                        {
                                            var variantInfo =
                                                await _catalogGateway.GetCatalogItemAsync(l.ProductId, l.VariantId, ct);
                                            variantName = variantInfo?.Name ?? variantName;
                                        }
                                    }
                                    catch
                                    {
                                        // اگر خطا در دریافت اطلاعات کاتالوگ رخ داد، از شناسه استفاده می‌کنیم
                                    }

                                    throw new InvalidOperationException(
                                        $"نمی‌توان از موجودی محصول '{productName}' (واریانت: {variantName}) کم کرد چون در انبار موجود نیست. " +
                                        $"برای کاهش موجودی، ابتدا باید محصول در انبار موجود باشد. " +
                                        $"(ProductId: {l.ProductId}, VariantId: {l.VariantId?.ToString() ?? "null"}, " +
                                        $"Lot: {l.LotNumber ?? "بدون لات"}, Expiry: {l.ExpiryDate?.ToString("yyyy-MM-dd") ?? "بدون تاریخ انقضا"})"
                                    );
                                }

                                // فقط برای QtyDelta مثبت (افزایش موجودی) StockItem جدید ایجاد می‌کنیم
                                // Fetch authoritative SKU from Catalog service via ACL
                                var catalogItem =
                                    await _catalogGateway.GetCatalogItemAsync(l.ProductId, l.VariantId, ct);
                                if (catalogItem is null)
                                {
                                    var errorMessage = l.VariantId.HasValue
                                        ? $"محصول با شناسه {l.ProductId} یا variant با شناسه {l.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است. " +
                                          $"لطفاً ابتدا محصول را در کاتالوگ فعال کنید."
                                        : $"محصول با شناسه {l.ProductId} در کاتالوگ یافت نشد یا غیرفعال است. " +
                                          $"لطفاً ابتدا محصول را در کاتالوگ فعال کنید.";
                                    throw new InvalidOperationException(errorMessage);
                                }

                                si = StockItem.Create(
                                    productId: l.ProductId,
                                    variantId: l.VariantId,
                                    warehouseId: adj.WarehouseId,
                                    sku: catalogItem.Sku,
                                    lotNumber: l.LotNumber,
                                    expiry: l.ExpiryDate
                                );
                                _db.StockItems.Add(si);
                            }

                            // اعمال تغییر مقدار
                            if (l.QtyDelta > 0)
                            {
                                si.Increase(l.QtyDelta);
                            }
                            else if (l.QtyDelta < 0)
                            {
                                // بررسی موجودی کافی قبل از کاهش
                                var decreaseAmount = -l.QtyDelta;
                                var available = si.Available;

                                if (decreaseAmount > available)
                                {
                                    string productName = si.ProductId.ToString();
                                    string variantName = si.VariantId?.ToString() ?? "بدون واریانت";

                                    try
                                    {
                                        var productInfo =
                                            await _catalogGateway.GetCatalogItemAsync(si.ProductId, si.VariantId, ct);
                                        productName = productInfo?.Name ?? productName;
                                        if (si.VariantId.HasValue)
                                        {
                                            var variantInfo =
                                                await _catalogGateway.GetCatalogItemAsync(si.ProductId, si.VariantId,
                                                    ct);
                                            variantName = variantInfo?.Name ?? variantName;
                                        }
                                    }
                                    catch
                                    {
                                        // اگر خطا در دریافت اطلاعات کاتالوگ رخ داد، از شناسه استفاده می‌کنیم
                                    }

                                    throw new InvalidOperationException(
                                        $"موجودی آزاد کافی نیست برای محصول '{productName}' (واریانت: {variantName}). " +
                                        $"موجودی آزاد: {available}, " +
                                        $"مقدار درخواستی برای کاهش: {decreaseAmount}, " +
                                        $"مجموع موجودی: {si.OnHand}, " +
                                        $"رزرو شده: {si.Reserved}, " +
                                        $"مسدود شده: {si.Blocked}. " +
                                        $"(SKU: {si.Sku}, Lot: {si.LotNumber ?? "بدون لات"}, " +
                                        $"Expiry: {si.ExpiryDate?.ToString("yyyy-MM-dd") ?? "بدون تاریخ انقضا"})"
                                    );
                                }

                                si.Decrease(decreaseAmount);
                            }

                            // ایجاد رکورد در دفتر موجودی بعد از موفقیت عملیات
                            var mtype = l.QtyDelta > 0
                                ? StockMovementType.AdjustmentPlus
                                : StockMovementType.AdjustmentMinus;

                            var ledger = StockLedgerEntry.Create(
                                timestampUtc: DateTime.UtcNow,
                                productId: si.ProductId,
                                variantId: si.VariantId,
                                warehouseId: si.WarehouseId,
                                lotNumber: si.LotNumber,
                                expiryDate: si.ExpiryDate,
                                deltaQty: l.QtyDelta,
                                type: mtype,
                                refDocType: nameof(Adjustment),
                                refDocId: adj.Id,
                                unitCost: null,
                                note: adj.Note
                            );
                            _db.StockLedger.Add(ledger);
                        }
                        catch (InvalidOperationException)
                        {
                            // Re-throw InvalidOperationException as-is (these are business logic errors)
                            throw;
                        }
                        catch (Exception ex) when (ex is not InvalidOperationException)
                        {
                            // Wrap other exceptions with more context
                            throw new InvalidOperationException(
                                $"خطا در پردازش خط اصلاح برای محصول {l.ProductId} (Variant: {l.VariantId?.ToString() ?? "null"}): {ex.Message}",
                                ex
                            );
                        }
                    }

                    adj.Post(req.WhenUtc);

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();

                    adj = await _db.Adjustments
                        .Include(a => a.Lines)
                        .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                        ?? throw new InvalidOperationException("سند اصلاح در تلاش مجدد یافت نشد.");

                    if (attempt == maxAttempts)
                        throw;
                }
            }
        });

        return Unit.Value;
    }
}
