using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Reports;

public sealed record DailySalesReportQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? SiteId = null,
    string? Status = null) : IRequest<IReadOnlyList<DailySalesReportItem>>;

public sealed class DailySalesReportHandler : IRequestHandler<DailySalesReportQuery, IReadOnlyList<DailySalesReportItem>>
{
    private readonly ISalesDbContext _db;

    public DailySalesReportHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<DailySalesReportItem>> Handle(DailySalesReportQuery req, CancellationToken ct)
    {
        var query = ReportQueryHelpers.BuildBaseOrders(_db.Orders.AsNoTracking(), req.FromUtc, req.ToUtc, req.SiteId, req.Status);

        var orders = await query
            .Select(o => new
            {
                Date = (o.PlacedAtUtc ?? o.CreatedAt).Date,
                OrderId = o.Id,
                o.Subtotal,
                o.DiscountTotal,
                o.FinalTotal,
                o.CashbackTotal
            })
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var lineCountsList = await _db.OrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.OrderId))
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, ItemsCount = g.Sum(l => l.Quantity) })
            .ToListAsync(ct);
        var lineCounts = lineCountsList.ToDictionary(x => x.OrderId, x => x.ItemsCount);

        var items = orders
            .GroupBy(o => o.Date)
            .Select(g => new DailySalesReportItem
            {
                Date = g.Key,
                OrdersCount = g.Count(),
                ItemsCount = g.Sum(o => lineCounts.GetValueOrDefault(o.OrderId, 0)),
                Subtotal = g.Sum(o => o.Subtotal),
                DiscountTotal = g.Sum(o => o.DiscountTotal),
                FinalTotal = g.Sum(o => o.FinalTotal),
                CashbackTotal = g.Sum(o => o.CashbackTotal)
            })
            .OrderBy(x => x.Date)
            .ToList();

        return items;
    }
}

public sealed record MonthlySalesReportQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? SiteId = null,
    string? Status = null) : IRequest<IReadOnlyList<MonthlySalesReportItem>>;

public sealed class MonthlySalesReportHandler : IRequestHandler<MonthlySalesReportQuery, IReadOnlyList<MonthlySalesReportItem>>
{
    private readonly ISalesDbContext _db;

    public MonthlySalesReportHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<MonthlySalesReportItem>> Handle(MonthlySalesReportQuery req, CancellationToken ct)
    {
        var query = ReportQueryHelpers.BuildBaseOrders(_db.Orders.AsNoTracking(), req.FromUtc, req.ToUtc, req.SiteId, req.Status);

        var orders = await query
            .Select(o => new
            {
                Year = (o.PlacedAtUtc ?? o.CreatedAt).Year,
                Month = (o.PlacedAtUtc ?? o.CreatedAt).Month,
                OrderId = o.Id,
                o.Subtotal,
                o.DiscountTotal,
                o.FinalTotal,
                o.CashbackTotal
            })
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var lineCountsList = await _db.OrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.OrderId))
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, ItemsCount = g.Sum(l => l.Quantity) })
            .ToListAsync(ct);
        var lineCounts = lineCountsList.ToDictionary(x => x.OrderId, x => x.ItemsCount);

        var items = orders
            .GroupBy(o => new { o.Year, o.Month })
            .Select(g => new MonthlySalesReportItem
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                OrdersCount = g.Count(),
                ItemsCount = g.Sum(o => lineCounts.GetValueOrDefault(o.OrderId, 0)),
                Subtotal = g.Sum(o => o.Subtotal),
                DiscountTotal = g.Sum(o => o.DiscountTotal),
                FinalTotal = g.Sum(o => o.FinalTotal),
                CashbackTotal = g.Sum(o => o.CashbackTotal)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return items;
    }
}

public sealed record SalesBySiteQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? Status = null) : IRequest<IReadOnlyList<SiteSalesReportItem>>;

public sealed class SalesBySiteHandler : IRequestHandler<SalesBySiteQuery, IReadOnlyList<SiteSalesReportItem>>
{
    private readonly ISalesDbContext _db;
    private readonly IStoreLookupGateway _stores;

    public SalesBySiteHandler(ISalesDbContext db, IStoreLookupGateway stores)
    {
        _db = db;
        _stores = stores;
    }

    public async Task<IReadOnlyList<SiteSalesReportItem>> Handle(SalesBySiteQuery req, CancellationToken ct)
    {
        var query = ReportQueryHelpers.BuildBaseOrders(_db.Orders.AsNoTracking(), req.FromUtc, req.ToUtc, null, req.Status);

        var orders = await query
            .Select(o => new
            {
                o.SiteId,
                OrderId = o.Id,
                o.FinalTotal
            })
            .ToListAsync(ct);

        var orderIds = orders.Select(o => o.OrderId).ToList();
        var lineCountsList = await _db.OrderLines.AsNoTracking()
            .Where(l => orderIds.Contains(l.OrderId))
            .GroupBy(l => l.OrderId)
            .Select(g => new { OrderId = g.Key, ItemsCount = g.Sum(l => l.Quantity) })
            .ToListAsync(ct);
        var lineCounts = lineCountsList.ToDictionary(x => x.OrderId, x => x.ItemsCount);

        var raw = orders
            .GroupBy(o => o.SiteId)
            .Select(g => new
            {
                SiteId = g.Key,
                OrdersCount = g.Count(),
                ItemsCount = g.Sum(o => lineCounts.GetValueOrDefault(o.OrderId, 0)),
                FinalTotal = g.Sum(o => o.FinalTotal)
            })
            .OrderByDescending(x => x.FinalTotal)
            .ToList();

        var storeMap = await _stores.GetStoresByIdsAsync(raw.Select(x => x.SiteId).Distinct().ToList(), ct);

        return raw.Select(x =>
        {
            storeMap.TryGetValue(x.SiteId, out var store);
            return new SiteSalesReportItem
            {
                SiteId = x.SiteId,
                SiteName = store?.Name,
                SiteDomain = store?.Domain,
                OrdersCount = x.OrdersCount,
                ItemsCount = x.ItemsCount,
                FinalTotal = x.FinalTotal
            };
        }).ToList();
    }
}

public sealed record SalesByProductQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? SiteId = null,
    string? Status = null) : IRequest<IReadOnlyList<ProductSalesReportItem>>;

public sealed class SalesByProductHandler : IRequestHandler<SalesByProductQuery, IReadOnlyList<ProductSalesReportItem>>
{
    private readonly ISalesDbContext _db;

    public SalesByProductHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductSalesReportItem>> Handle(SalesByProductQuery req, CancellationToken ct)
    {
        var orders = ReportQueryHelpers.BuildBaseOrders(_db.Orders.AsNoTracking(), req.FromUtc, req.ToUtc, req.SiteId, req.Status);

        var items = await (
            from o in orders
            join l in _db.OrderLines.AsNoTracking() on o.Id equals l.OrderId
            group new { o.Id, l } by l.SkuId into g
            select new ProductSalesReportItem
            {
                SkuId = g.Key,
                OrdersCount = g.Select(x => x.Id).Distinct().Count(),
                Quantity = g.Sum(x => x.l.Quantity),
                Revenue = g.Sum(x => x.l.FinalUnitPrice * x.l.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .ToListAsync(ct);

        return items;
    }
}

public sealed record SalesByCustomerQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? SiteId = null,
    string? Status = null) : IRequest<IReadOnlyList<CustomerSalesReportItem>>;

public sealed class SalesByCustomerHandler : IRequestHandler<SalesByCustomerQuery, IReadOnlyList<CustomerSalesReportItem>>
{
    private readonly ISalesDbContext _db;

    public SalesByCustomerHandler(ISalesDbContext db) => _db = db;

    public async Task<IReadOnlyList<CustomerSalesReportItem>> Handle(SalesByCustomerQuery req, CancellationToken ct)
    {
        var query = ReportQueryHelpers.BuildBaseOrders(_db.Orders.AsNoTracking(), req.FromUtc, req.ToUtc, req.SiteId, req.Status);

        return await query
            .GroupBy(o => o.UserId)
            .Select(g => new CustomerSalesReportItem
            {
                UserId = g.Key,
                OrdersCount = g.Count(),
                FinalTotal = g.Sum(x => x.FinalTotal)
            })
            .OrderByDescending(x => x.FinalTotal)
            .ToListAsync(ct);
    }
}

internal static class ReportQueryHelpers
{
    private static readonly OrderStatus[] ReportStatuses =
    {
        OrderStatus.Placed,
        OrderStatus.Shipped,
        OrderStatus.Delivered,
        OrderStatus.Returned,
        OrderStatus.Refunded
    };

    public static IQueryable<Order> BuildBaseOrders(
        IQueryable<Order> src,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? siteId,
        string? status)
    {
        var query = src.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                // no status filter - show all orders
            }
            else if (Enum.TryParse<OrderStatus>(status.Trim(), true, out var parsed))
            {
                query = query.Where(o => o.Status == parsed);
            }
            else
            {
                // Invalid status value - default to showing all orders
                // no status filter
            }
        }
        else
        {
            // When status is not provided, show all orders (not just ReportStatuses)
            // no status filter
        }

        if (siteId.HasValue)
            query = query.Where(o => o.SiteId == siteId.Value);

        if (fromUtc.HasValue)
        {
            var from = fromUtc.Value.Kind == DateTimeKind.Utc
                ? fromUtc.Value
                : DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => (o.PlacedAtUtc ?? o.CreatedAt) >= from);
        }

        if (toUtc.HasValue)
        {
            var to = toUtc.Value.Kind == DateTimeKind.Utc
                ? toUtc.Value
                : DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => (o.PlacedAtUtc ?? o.CreatedAt) <= to);
        }

        return query;
    }
}
