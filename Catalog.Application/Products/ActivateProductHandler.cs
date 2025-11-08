using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class ActivateProductHandler : IRequestHandler<ActivateProductCommand, Unit>
{
    private readonly DbContext _db;
    public ActivateProductHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(ActivateProductCommand req, CancellationToken ct)
    {
        // بارگذاری محصول با روابط لازم
        var product = await _db.Set<Product>()
            .Include(p => p.Images)
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product is null)
            throw new InvalidOperationException("Product not found.");

        // قوانین فعال‌سازی:
        // 1) حداقل یک دسته
        if (product.Categories.Count == 0)
            throw new InvalidOperationException("Product must have at least one category.");

        // 2) حداقل یک تصویر و تنظیم MainImageId
        if (!product.Images.Any())
            throw new InvalidOperationException("Product must have at least one image before activation.");

        if (product.MainImageId is null)
        {
            // اگر قبلاً ست نشده، اولین تصویر را Main کن
            var firstImgId = product.Images.OrderBy(i => i.SortOrder).First().Id;
            product.SetMainImage(firstImgId);
        }

        // 3) تغییر وضعیت
        product.SetStatus(ProductStatus.Active);

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

}