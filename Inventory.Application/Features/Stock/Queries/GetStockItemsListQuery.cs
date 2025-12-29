using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

public sealed record GetStockItemsListQuery(
    Guid? WarehouseId = null,
    Guid? ProductId = null,
    Guid? VariantId = null,
    Guid? ShelfId = null,
    string? Search = null,
    bool? HasStock = null, // true = موجودی > 0
    bool? ShelvedOnly = null, // true => فقط آیتم‌های دارای ShelfId
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
    int AvailableSerialsCount,
    string? BlockReason,
    Guid? ShelfId,
    string? ShelfName,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed class GetStockItemsListHandler : IRequestHandler<GetStockItemsListQuery, StockItemsListResult>
{
    private readonly IInventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    public GetStockItemsListHandler(IInventoryDbContext db, ICatalogGateway catalogGateway)
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
        else if (req.ShelvedOnly == true)
            query = query.Where(si => si.ShelfId != null);

        // Search can include product/variant name (from Catalog) so the final filtering may need in-memory evaluation.

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

        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var searchTermLower = !string.IsNullOrWhiteSpace(req.Search) ? req.Search.Trim().ToLower() : null;

        var productNameCache = new Dictionary<Guid, string?>();
        var variantValueCache = new Dictionary<(Guid ProductId, Guid VariantId), (string? ProductName, string? VariantValue)>();

        async Task<(string? ProductName, string? VariantValue)> GetCatalogNamesAsync(Guid productId, Guid? variantId)
        {
            string? productName = null;
            string? variantValue = null;

            if (!productNameCache.TryGetValue(productId, out productName))
            {
                using var perCallTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, perCallTimeout.Token);
                try
                {
                    var info = await _catalogGateway.GetCatalogItemAsync(productId, null, linkedCts.Token);
                    productName = info?.Name;
                }
                catch (OperationCanceledException) { }
                catch { }

                productNameCache[productId] = productName;
            }

            if (variantId.HasValue)
            {
                var key = (productId, variantId.Value);
                if (variantValueCache.TryGetValue(key, out var cached))
                    return cached;

                using var perCallTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, perCallTimeout.Token);
                try
                {
                    var variantInfo = await _catalogGateway.GetCatalogItemAsync(productId, variantId.Value, linkedCts.Token);
                    if (variantInfo?.Name != null)
                    {
                        var parts = variantInfo.Name.Split(" - ", 2, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            productName ??= parts[0];
                            variantValue = parts[1];
                        }
                        else
                        {
                            variantValue = variantInfo.Name;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch { }

                if (productNameCache[productId] is null && productName is not null)
                    productNameCache[productId] = productName;

                var result = (productName, variantValue);
                variantValueCache[key] = result;
                return result;
            }

            return (productName, variantValue);
        }

        if (searchTermLower is null)
        {
            var totalCount = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

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

            var rawItemIds = rawItems.Select(x => x.Id).ToList();
            var serialCounts = rawItemIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await _db.StockItemSerials
                    .AsNoTracking()
                    .Where(s => s.StockItemId != null &&
                                rawItemIds.Contains(s.StockItemId.Value) &&
                                s.Status == StockSerialStatus.Available)
                    .GroupBy(s => s.StockItemId!.Value)
                    .Select(g => new { StockItemId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.StockItemId, x => x.Count, ct);

            var warehouseIds = rawItems.Select(x => x.WarehouseId).Distinct().ToList();
            var warehouses = await _db.Warehouses
                .AsNoTracking()
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

            var shelfIds = rawItems.Where(x => x.ShelfId.HasValue).Select(x => x.ShelfId!.Value).Distinct().ToList();
            var shelves = shelfIds.Count == 0
                ? new Dictionary<Guid, string>()
                : await _db.StockShelves
                    .AsNoTracking()
                    .Where(s => shelfIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

            var items = new List<StockItemListItemDto>(rawItems.Count);
            foreach (var item in rawItems)
            {
                var (productName, variantValue) = await GetCatalogNamesAsync(item.ProductId, item.VariantId);
                var available = item.OnHand - item.Reserved - item.Blocked;
                var availableSerialsCount = serialCounts.TryGetValue(item.Id, out var count) ? count : 0;
                items.Add(new StockItemListItemDto(
                    item.Id,
                    item.ProductId,
                    item.VariantId,
                    item.WarehouseId,
                    warehouses.TryGetValue(item.WarehouseId, out var whName) ? whName : null,
                    item.Sku,
                    productName,
                    variantValue,
                    item.LotNumber,
                    item.ExpiryDate,
                    item.OnHand,
                    item.Reserved,
                    item.Blocked,
                    available,
                    availableSerialsCount,
                    item.BlockReason,
                    item.ShelfId,
                    item.ShelfId.HasValue && shelves.TryGetValue(item.ShelfId.Value, out var shelfName) ? shelfName : null,
                    item.CreatedAt,
                    item.UpdatedAt
                ));
            }

            return new StockItemsListResult(items, totalCount, page, pageSize, totalPages);
        }

        // When search is present, we must filter after catalog enrichment; so we build the matched set first,
        // then paginate on the filtered result so TotalCount/TotalPages are correct.
        var allRawItems = await query
            .OrderByDescending(si => si.UpdatedAt)
            .ThenByDescending(si => si.CreatedAt)
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

        var allRawItemIds = allRawItems.Select(x => x.Id).ToList();
        var allSerialCounts = allRawItemIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.StockItemSerials
                .AsNoTracking()
                .Where(s => s.StockItemId != null &&
                            allRawItemIds.Contains(s.StockItemId.Value) &&
                            s.Status == StockSerialStatus.Available)
                .GroupBy(s => s.StockItemId!.Value)
                .Select(g => new { StockItemId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.StockItemId, x => x.Count, ct);

        var allWarehouseIds = allRawItems.Select(x => x.WarehouseId).Distinct().ToList();
        var allWarehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => allWarehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        var allShelfIds = allRawItems.Where(x => x.ShelfId.HasValue).Select(x => x.ShelfId!.Value).Distinct().ToList();
        var allShelves = allShelfIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.StockShelves
                .AsNoTracking()
                .Where(s => allShelfIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var matched = new List<StockItemListItemDto>(allRawItems.Count);
        foreach (var item in allRawItems)
        {
            var (productName, variantValue) = await GetCatalogNamesAsync(item.ProductId, item.VariantId);

            var matchesSku = item.Sku.ToLower().Contains(searchTermLower);
            var matchesLot = item.LotNumber != null && item.LotNumber.ToLower().Contains(searchTermLower);
            var matchesProductName = productName != null && productName.ToLower().Contains(searchTermLower);
            var matchesVariant = variantValue != null && variantValue.ToLower().Contains(searchTermLower);

            if (!matchesSku && !matchesLot && !matchesProductName && !matchesVariant)
                continue;

            var available = item.OnHand - item.Reserved - item.Blocked;
            var availableSerialsCount = allSerialCounts.TryGetValue(item.Id, out var count) ? count : 0;
            matched.Add(new StockItemListItemDto(
                item.Id,
                item.ProductId,
                item.VariantId,
                item.WarehouseId,
                allWarehouses.TryGetValue(item.WarehouseId, out var whName) ? whName : null,
                item.Sku,
                productName,
                variantValue,
                item.LotNumber,
                item.ExpiryDate,
                item.OnHand,
                item.Reserved,
                item.Blocked,
                available,
                availableSerialsCount,
                item.BlockReason,
                item.ShelfId,
                item.ShelfId.HasValue && allShelves.TryGetValue(item.ShelfId.Value, out var shelfName) ? shelfName : null,
                item.CreatedAt,
                item.UpdatedAt
            ));
        }

        var filteredTotalCount = matched.Count;
        var filteredTotalPages = (int)Math.Ceiling(filteredTotalCount / (double)pageSize);
        var filteredPage = Math.Clamp(req.Page, 1, Math.Max(1, filteredTotalPages));

        var paged = matched
            .Skip((filteredPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new StockItemsListResult(paged, filteredTotalCount, filteredPage, pageSize, filteredTotalPages);
    }
}
