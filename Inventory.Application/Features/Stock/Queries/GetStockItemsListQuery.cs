using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

public sealed record GetStockItemsListQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
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

        // Get shelves for names
        var shelfIds = await query.Where(si => si.ShelfId != null).Select(si => si.ShelfId!.Value).Distinct().ToListAsync(ct);
        var shelves = await _db.StockShelves
            .AsNoTracking()
            .Where(s => shelfIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

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
                si.Blocked,
                si.BlockReason,
                si.ShelfId,
                si.CreatedAt,
                si.UpdatedAt
            })
            .ToListAsync(ct);

        // Validate products exist in catalog and get product/variant names
        // Also filter by product name if search term is provided
        var searchTermLower = !string.IsNullOrWhiteSpace(req.Search) ? req.Search.Trim().ToLower() : null;
        var validItems = new List<StockItemListItemDto>();
        
        foreach (var item in rawItems)
        {
            try
            {
                // Get product name (without variant)
                var productInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, null, ct);
                if (productInfo is null)
                    continue; // Skip invalid products

                string? variantValue = null;
                string? productName = productInfo.Name;

                // If variant exists, get variant-specific name
                if (item.VariantId.HasValue)
                {
                    var variantInfo = await _catalogGateway.GetCatalogItemAsync(item.ProductId, item.VariantId, ct);
                    if (variantInfo is null)
                        continue; // Skip if variant not found

                    // Extract variant value from name (format: "ProductName - VariantValue")
                    if (variantInfo.Name.Contains(" - "))
                    {
                        var parts = variantInfo.Name.Split(" - ", 2);
                        productName = parts[0];
                        variantValue = parts.Length > 1 ? parts[1] : null;
                    }
                    else
                    {
                        variantValue = variantInfo.Name;
                    }
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
            catch
            {
                // Skip if catalog validation fails
                continue;
            }
        }

        // Adjust total count (we filtered out invalid items)
        var adjustedTotal = validItems.Count < pageSize && page == 1
            ? validItems.Count
            : totalCount; // Approximation - in production you might want to count valid items separately

        return new StockItemsListResult(
            validItems,
            adjustedTotal,
            page,
            pageSize,
            totalPages
        );
    }
}

