using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

public sealed record GetStockItemsListQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    Guid? ShelfId = null,
    string? Search = null,
    bool? HasStock = null, // true = فقط موجودی > 0
    int Page = 1,
    int PageSize = 20
) : IRequest<StockItemsListResult>;

public sealed record StockItemsListResult(
    IReadOnlyList<StockItemListItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record StockItemListItemDto(
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
    decimal Available,
    string? BlockReason,
    Guid? ShelfId,
    string? ShelfName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed class GetStockItemsListHandler : IRequestHandler<GetStockItemsListQuery, StockItemsListResult>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    public GetStockItemsListHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<StockItemsListResult> Handle(GetStockItemsListQuery req, CancellationToken ct)
    {
        var query = _db.StockItems.AsNoTracking().AsQueryable();

        // Apply filters
        if (req.WarehouseId.HasValue)
            query = query.Where(si => si.WarehouseId == req.WarehouseId.Value);

        if (req.ProductId.HasValue)
            query = query.Where(si => si.ProductId == req.ProductId.Value);

        if (req.VariantId.HasValue)
            query = query.Where(si => si.VariantId == req.VariantId.Value);

        if (req.ShelfId.HasValue)
            query = query.Where(si => si.ShelfId == req.ShelfId.Value);

        // Note: Product name search will be done after fetching product names from catalog
        // We don't filter by search term here - we'll filter after getting product names

        // By default, only show items with stock (OnHand > 0)
        // If HasStock is explicitly set to false, show items with zero stock
        if (req.HasStock.HasValue)
        {
            if (req.HasStock.Value)
                query = query.Where(si => si.OnHand > 0);
            else
                query = query.Where(si => si.OnHand == 0);
        }
        else
        {
            // Default behavior: only show items with stock
            query = query.Where(si => si.OnHand > 0);
        }

        // Get warehouses for names (before pagination to avoid multiple queries)
        var warehouseIds = await query.Select(si => si.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        // Get shelves for names (before pagination to avoid multiple queries)
        var shelfIds = await query.Where(si => si.ShelfId != null).Select(si => si.ShelfId!.Value).Distinct().ToListAsync(ct);
        var shelves = await _db.StockShelves
            .AsNoTracking()
            .Where(s => shelfIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get paginated results (ordered)
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
                si.Blocked,
                si.BlockReason,
                si.ShelfId,
                si.CreatedAt,
                si.UpdatedAt
            })
            .ToListAsync(ct);

        // Get product names from catalog (اختیاری؛ اگر کاتالوگ در دسترس نباشد، آیتم را حذف نمی‌کنیم)
        var searchTermLower = !string.IsNullOrWhiteSpace(req.Search) ? req.Search.Trim().ToLower() : null;
        var validItems = new List<StockItemListItemDto>();
        
        foreach (var item in rawItems)
        {
            string? productName = null;
            string? variantValue = null;

            // Get product name from catalog (with timeout)
            using var catalogTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, catalogTimeoutCts.Token);

            try
            {
                // Get product name (without variant)
                var productInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, null, linkedCts.Token);
                productName = productInfo?.Name;

                // If variant exists, get variant-specific name
                if (item.VariantId.HasValue)
                {
                    var variantInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, item.VariantId, linkedCts.Token);
                    if (variantInfo?.Name != null)
                    {
                        // Extract variant value from name (format: "ProductName - VariantValue")
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
                // Timeout - continue without catalog info
            }
            catch
            {
                // در صورت خطا در کاتالوگ، ادامه می‌دهیم و آیتم را حذف نمی‌کنیم
            }

            // Filter by search term if provided - check all fields
            if (searchTermLower != null)
            {
                var matchesSku = item.Sku.ToLower().Contains(searchTermLower);
                var matchesLot = item.LotNumber != null && item.LotNumber.ToLower().Contains(searchTermLower);
                var matchesProductName = productName != null && productName.ToLower().Contains(searchTermLower);
                var matchesVariant = variantValue != null && variantValue.ToLower().Contains(searchTermLower);

                // If search term doesn't match any field, skip this item
                if (!matchesSku && !matchesLot && !matchesProductName && !matchesVariant)
                    continue;
            }

            var available = item.OnHand - item.Reserved - item.Blocked;
            validItems.Add(new StockItemListItemDto(
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
                available,
                item.BlockReason,
                item.ShelfId,
                item.ShelfId.HasValue && shelves.TryGetValue(item.ShelfId.Value, out var shelfName) ? shelfName : null,
                item.CreatedAt,
                item.UpdatedAt
            ));
        }

        // Return results with original total count and total pages
        // Note: totalCount and totalPages are based on database query, not filtered by catalog
        return new StockItemsListResult(
            validItems,
            totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

