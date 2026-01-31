using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;
using Sales.Domain.Orders;

namespace Sales.Application.Features.Orders.Queries;

public sealed record ListOrdersQuery(
    Guid? SiteId = null,
    Guid? UserId = null,
    string? Status = null,
    string? Search = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<OrderListResult>;

public sealed class ListOrdersHandler : IRequestHandler<ListOrdersQuery, OrderListResult>
{
    private readonly ISalesDbContext _db;
    private readonly IStoreLookupGateway _stores;

    public ListOrdersHandler(ISalesDbContext db, IStoreLookupGateway stores)
    {
        _db = db;
        _stores = stores;
    }

    public async Task<OrderListResult> Handle(ListOrdersQuery req, CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (req.SiteId.HasValue)
            query = query.Where(o => o.SiteId == req.SiteId.Value);

        if (req.UserId.HasValue)
            query = query.Where(o => o.UserId == req.UserId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<OrderStatus>(req.Status.Trim(), true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.Trim();
            if (Guid.TryParse(s, out var guid))
            {
                query = query.Where(o =>
                    o.Id == guid ||
                    o.PricingQuoteId == guid ||
                    (o.UserId.HasValue && o.UserId.Value == guid));
            }
        }

        if (req.FromUtc.HasValue)
        {
            var from = req.FromUtc.Value.Kind == DateTimeKind.Utc
                ? req.FromUtc.Value
                : DateTime.SpecifyKind(req.FromUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt >= from);
        }

        if (req.ToUtc.HasValue)
        {
            var to = req.ToUtc.Value.Kind == DateTimeKind.Utc
                ? req.ToUtc.Value
                : DateTime.SpecifyKind(req.ToUtc.Value, DateTimeKind.Utc);
            query = query.Where(o => o.CreatedAt <= to);
        }

        var totalCount = await query.CountAsync(ct);
        var pageSize = Math.Clamp(req.PageSize, 1, 100);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var page = Math.Clamp(req.Page, 1, Math.Max(1, totalPages));

        var rawItems = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.SiteId,
                o.UserId,
                o.OrderNumber,
                o.PricingQuoteId,
                o.Currency,
                Status = o.Status.ToString(),
                o.CreatedAt,
                o.UpdatedAt,
                o.PlacedAtUtc,
                o.PaymentFailedAtUtc,
                o.PaymentFailureReason,
                o.Subtotal,
                o.DiscountTotal,
                o.FinalTotal,
                o.CashbackTotal,
                LinesCount = o.Lines.Count
            })
            .ToListAsync(ct);

        var siteIds = rawItems.Select(x => x.SiteId).Distinct().ToList();
        var storeMap = await _stores.GetStoresByIdsAsync(siteIds, ct);

        var items = rawItems.Select(o =>
        {
            storeMap.TryGetValue(o.SiteId, out var store);
            return new OrderListItemDto
            {
                Id = o.Id,
                SiteId = o.SiteId,
                UserId = o.UserId,
                SiteName = store?.Name,
                SiteDomain = store?.Domain,
                OrderNumber = o.OrderNumber,
                PricingQuoteId = o.PricingQuoteId,
                Currency = o.Currency,
                Status = o.Status,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                PlacedAtUtc = o.PlacedAtUtc,
                PaymentFailedAtUtc = o.PaymentFailedAtUtc,
                PaymentFailureReason = o.PaymentFailureReason,
                Subtotal = o.Subtotal,
                DiscountTotal = o.DiscountTotal,
                FinalTotal = o.FinalTotal,
                CashbackTotal = o.CashbackTotal,
                LinesCount = o.LinesCount
            };
        }).ToList();

        return new OrderListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }
}
