using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

/// <summary>
/// Query to get StockItems that don't have a Shelf assigned (ShelfId is null)
/// These are items that need to be placed in shelves (Put-away operation)
/// </summary>
public sealed record GetUnassignedStockItemsQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<UnassignedStockItemsResult>;

public sealed record UnassignedStockItemsResult(
    IReadOnlyList<UnassignedStockItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record UnassignedStockItemDto(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    Guid WarehouseId,
    string? WarehouseName,
    string Sku,
    string? ProductName,
    string? VariantValue,
    string? LotNumber,
    DateTime? ExpiryDate,
    decimal OnHand,
    decimal Reserved,
    decimal Blocked,
    decimal Available
);

public sealed class GetUnassignedStockItemsHandler : IRequestHandler<GetUnassignedStockItemsQuery, UnassignedStockItemsResult>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    public GetUnassignedStockItemsHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<UnassignedStockItemsResult> Handle(GetUnassignedStockItemsQuery req, CancellationToken ct)
    {
        // Only get StockItems without Shelf (ShelfId is null) and Blocked (new items that need to be shelved)
        var query = _db.StockItems
            .AsNoTracking()
            .Where(si => si.ShelfId == null && si.Blocked > 0)  // فقط موجودی‌های مسدود و بدون قفسه
            .AsQueryable();

        // Apply filters
        if (req.WarehouseId.HasValue)
            query = query.Where(si => si.WarehouseId == req.WarehouseId.Value);

        if (req.ProductId.HasValue)
            query = query.Where(si => si.ProductId == req.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var searchTerm = req.Search.Trim().ToLower();
            query = query.Where(si =>
                si.Sku.ToLower().Contains(searchTerm) ||
                (si.LotNumber != null && si.LotNumber.ToLower().Contains(searchTerm))
            );
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get warehouses for names
        var warehouseIds = await query.Select(si => si.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get paginated results
        var rawItems = await query
            .OrderByDescending(si => si.UpdatedAt)
            .ThenByDescending(si => si.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(si => new
            {
                si.Id,
                si.ProductId,
                si.VariantId,
                si.WarehouseId,
                si.Sku,
                si.LotNumber,
                si.ExpiryDate,
                si.OnHand,
                si.Reserved,
                si.Blocked
            })
            .ToListAsync(ct);

        // Get product names from catalog (اختیاری؛ اگر کاتالوگ در دسترس نباشد، آیتم را حذف نمی‌کنیم)
        var validItems = new List<UnassignedStockItemDto>();
        var searchTermLower = !string.IsNullOrWhiteSpace(req.Search) ? req.Search.Trim().ToLower() : null;

        // Timeout کوتاه برای درخواست‌های Catalog API (2 ثانیه)
        using var catalogTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        catalogTimeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

        foreach (var item in rawItems)
        {
            string? productName = null;
            string? variantValue = null;

            try
            {
                // استفاده از timeout کوتاه برای جلوگیری از hang شدن
                var productInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, null, catalogTimeoutCts.Token);
                productName = productInfo?.Name;

                if (item.VariantId.HasValue)
                {
                    var variantInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, item.VariantId, catalogTimeoutCts.Token);
                    if (variantInfo?.Name != null)
                    {
                        if (variantInfo.Name.Contains(" - "))
                        {
                            var parts = variantInfo.Name.Split(" - ", 2);
                            productName ??= parts[0];
                            variantValue = parts.Length > 1 ? parts[1] : null;
                        }
                        else
                        {
                            variantValue = variantInfo.Name;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout یا cancellation - ادامه می‌دهیم بدون اطلاعات محصول
            }
            catch
            {
                // در صورت خطا در کاتالوگ، ادامه می‌دهیم و آیتم را حذف نمی‌کنیم
            }

            // Filter by search term (SKU / Lot / ProductName / Variant)
            if (searchTermLower != null)
            {
                var matchesSku = item.Sku.ToLower().Contains(searchTermLower);
                var matchesLot = item.LotNumber != null && item.LotNumber.ToLower().Contains(searchTermLower);
                var matchesProductName = productName != null && productName.ToLower().Contains(searchTermLower);
                var matchesVariant = variantValue != null && variantValue.ToLower().Contains(searchTermLower);

                if (!matchesSku && !matchesLot && !matchesProductName && !matchesVariant)
                    continue;
            }

            var available = item.OnHand - item.Reserved - item.Blocked;
            validItems.Add(new UnassignedStockItemDto(
                item.Id,
                item.ProductId,
                item.VariantId,
                item.WarehouseId,
                warehouses.TryGetValue(item.WarehouseId, out var name) ? name : null,
                item.Sku,
                productName,
                variantValue,
                item.LotNumber,
                item.ExpiryDate,
                item.OnHand,
                item.Reserved,
                item.Blocked,
                available
            ));
        }

        return new UnassignedStockItemsResult(
            validItems,
            validItems.Count < pageSize && page == 1 ? validItems.Count : totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

