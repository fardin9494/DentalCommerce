using Inventory.Application.Common.Interfaces;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Stock.Queries;

/// <summary>
/// Query to get list of products/variants that exist in StockItems for a warehouse
/// Used for adjustment operations where we can only adjust existing stock items
/// </summary>
public sealed record GetStockProductsQuery(
    Guid WarehouseId,
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<StockProductsResult>;

public sealed record StockProductsResult(
    IReadOnlyList<StockProductDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

public sealed record StockProductDto(
    Guid ProductId,
    Guid? VariantId,
    string Sku,
    string? ProductName,
    string? VariantValue,
    decimal TotalOnHand,
    decimal TotalAvailable
);

public sealed class GetStockProductsHandler : IRequestHandler<GetStockProductsQuery, StockProductsResult>
{
    private readonly IInventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    public GetStockProductsHandler(IInventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<StockProductsResult> Handle(GetStockProductsQuery req, CancellationToken ct)
    {
        // Get unique ProductId/VariantId combinations from StockItems in the warehouse
        var stockItemsQuery = _db.StockItems
            .AsNoTracking()
            .Where(si => si.WarehouseId == req.WarehouseId)
            .GroupBy(si => new { si.ProductId, si.VariantId })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.VariantId,
                Sku = g.First().Sku, // Use first SKU (they should be the same for same product/variant)
                TotalOnHand = g.Sum(si => si.OnHand),
                TotalAvailable = g.Sum(si => si.OnHand - si.Reserved - si.Blocked)
            })
            .AsQueryable();

        // Get total count
        var totalCount = await stockItemsQuery.CountAsync(ct);

        // Calculate pagination
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        // Get paginated results
        var rawItems = await stockItemsQuery
            .OrderBy(x => x.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Get product names from catalog and filter by search term
        var searchTermLower = !string.IsNullOrWhiteSpace(req.Search) ? req.Search.Trim().ToLower() : null;
        var validItems = new List<StockProductDto>();

        foreach (var item in rawItems)
        {
            try
            {
                // Get product name
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
                        continue;

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

                // Filter by search term if provided
                if (searchTermLower != null)
                {
                    var matchesSku = item.Sku.ToLower().Contains(searchTermLower);
                    var matchesProductName = productName != null && productName.ToLower().Contains(searchTermLower);
                    var matchesVariant = variantValue != null && variantValue.ToLower().Contains(searchTermLower);

                    if (!matchesSku && !matchesProductName && !matchesVariant)
                        continue;
                }

                validItems.Add(new StockProductDto(
                    item.ProductId,
                    item.VariantId,
                    item.Sku,
                    productName,
                    variantValue,
                    item.TotalOnHand,
                    item.TotalAvailable
                ));
            }
            catch
            {
                continue;
            }
        }

        return new StockProductsResult(
            validItems,
            validItems.Count < pageSize && page == 1 ? validItems.Count : totalCount,
            page,
            pageSize,
            totalPages
        );
    }
}

