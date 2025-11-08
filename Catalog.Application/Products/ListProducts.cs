using Catalog.Application.Medias;
using Catalog.Domain.Brands;            // Brand
using Catalog.Domain.Products;          // Product, ProductImage, ProductStore, ProductCategory (Link)
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalog.Application.Products
{
    // ورودیِ جست‌وجو/فیلتر/صفحه‌بندی/مرتب‌سازی
    public sealed record ListProductsQuery(
        int Page = 1,
        int PageSize = 20,
        string? Search = null,           // روی Name/Code/DefaultSlug (LIKE)
        Guid? BrandId = null,
        Guid? CategoryId = null,         // لینک به کتگوری (ProductCategory)
        Guid? StoreId = null,            // لینک به استور (ProductStore)
        bool? VisibleInStore = null,     // فیلتر روی ProductStore.IsVisible (در صورت تعیین StoreId)
        string? Sort = null              // name|-name|created|-created|updated|-updated (پیش‌فرض: -created)
    ) : IRequest<PagedResult<ProductListItemDto>>;

    // خروجی صفحه‌بندی
    public sealed class PagedResult<T>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int Total { get; init; }
        public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    }

    // آیتم خروجی برای گرید ادمین
   

    public sealed class ListProductsHandler : IRequestHandler<ListProductsQuery, PagedResult<ProductListItemDto>>
    {
        private readonly DbContext _db;
        private readonly IFileStorage _fs;

        public ListProductsHandler(DbContext db, IFileStorage fs)
        {
            _db = db;
            _fs = fs;
        }

        public async Task<PagedResult<ProductListItemDto>> Handle(ListProductsQuery q, CancellationToken ct)
        {
            // 1) پایهٔ کوئری
            var src = _db.Set<Product>().AsNoTracking();

            // 2) جست‌وجو (LIKE) روی Name/Code/DefaultSlug
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var s = $"%{q.Search.Trim()}%";
                src = src.Where(p =>
                    EF.Functions.Like(p.Name, s) ||
                    EF.Functions.Like(p.Code, s) ||
                    EF.Functions.Like(p.DefaultSlug, s));
            }

            // 3) برند
            if (q.BrandId is Guid bid)
                src = src.Where(p => p.BrandId == bid);

            // 4) کتگوری (لینک محصول-کتگوری)
            if (q.CategoryId is Guid cid)
                src = src.Where(p => p.Categories.Any(c => c.CategoryId == cid));

            // 5) استور + وضعیت نمایش در استور
            if (q.StoreId is Guid sid)
            {
                src = src.Where(p => p.Stores.Any(ps => ps.StoreId == sid));
                if (q.VisibleInStore is bool vis)
                    src = src.Where(p => p.Stores.Any(ps => ps.StoreId == sid && ps.IsVisible == vis));
            }

            // 6) مرتب‌سازی
            src = ApplySort(src, q.Sort);

            // 7) شمارش کل قبل از صفحه‌بندی
            var total = await src.CountAsync(ct);

            // 8) صفحه‌بندی امن
            var page = Math.Max(1, q.Page);
            var pageSize = Math.Clamp(q.PageSize, 1, 200);
            var skip = (page - 1) * pageSize;

            // 9) واکشی «هسته» برای صفحهٔ جاری
            var core = await src
                .Skip(skip)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Code,
                    p.DefaultSlug,
                    p.BrandId,
                    p.PrimaryCategoryId,
                    Status = p.Status.ToString(),
                    p.MainImageId,
                    p.CreatedAt,
                    p.UpdatedAt
                })
                .ToListAsync(ct);

            // 10) واکشی نام برندها در یک مرحله (جلوگیری از N+1)
            var brandIds = core.Where(x => x.BrandId != null).Select(x => x.BrandId!).Distinct().ToList();
            var brands = await _db.Set<Brand>()
                .AsNoTracking()
                .Where(b => brandIds.Contains(b.Id))
                .Select(b => new { b.Id, b.Name })
                .ToDictionaryAsync(b => b.Id, ct);

            // 11) واکشی مسیر تصویر اصلی در یک مرحله
            var mainIds = core.Where(x => x.MainImageId != null).Select(x => x.MainImageId!.Value).Distinct().ToList();
            var imageMap = await _db.Set<ProductImage>()
                .AsNoTracking()
                .Where(i => mainIds.Contains(i.Id))
                .Select(i => new { i.Id, i.Url })
                .ToDictionaryAsync(i => i.Id, ct);

            // 12) مونتاژ DTO در مموری + ساخت URL عمومی
            var items = core.Select(x => new ProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                DefaultSlug = x.DefaultSlug,
                BrandId = x.BrandId,
                BrandName = (x.BrandId != null && brands.TryGetValue(x.BrandId, out var b)) ? b.Name : null,
                PrimaryCategoryId = x.PrimaryCategoryId,
                Status = x.Status,
                MainImageId = x.MainImageId,
                MainImageUrl = (x.MainImageId != null && imageMap.TryGetValue(x.MainImageId.Value, out var im))
                               ? _fs.GetPublicUrl(im.Url)
                               : null,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();

            return new PagedResult<ProductListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            };
        }

        private static IQueryable<Product> ApplySort(IQueryable<Product> q, string? sort)
        {
            var key = (sort ?? "-created").Trim().ToLowerInvariant();
            var desc = key.StartsWith("-");
            var field = desc ? key[1..] : key;

            return field switch
            {
                "name" => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
                "created" => desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
                "updated" => desc ? q.OrderByDescending(p => p.UpdatedAt) : q.OrderBy(p => p.UpdatedAt),
                "code" => desc ? q.OrderByDescending(p => p.Code) : q.OrderBy(p => p.Code),
                _ => q.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}
