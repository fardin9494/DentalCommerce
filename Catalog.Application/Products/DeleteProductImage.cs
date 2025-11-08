using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Application.Medias;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<Unit>;

public sealed class DeleteProductImageHandler : IRequestHandler<DeleteProductImageCommand, Unit>
{
    private readonly DbContext _db;
    private readonly IImageProcessor _proc;
    public DeleteProductImageHandler(DbContext db, IImageProcessor proc)
    { _db = db; _proc = proc; }

    public async Task<Unit> Handle(DeleteProductImageCommand req, CancellationToken ct)
    {
        var img = await _db.Set<ProductImage>().FirstOrDefaultAsync(i => i.Id == req.ImageId && i.ProductId == req.ProductId, ct)
                  ?? throw new InvalidOperationException("Image not found.");

        var p = await _db.Set<Product>().FirstOrDefaultAsync(x => x.Id == req.ProductId, ct)
                ?? throw new InvalidOperationException("Product not found.");

        // حذف رکورد
        _db.Set<ProductImage>().Remove(img);
        await _db.SaveChangesAsync(ct);

        // اسم thumbnailها را براساس نام فایل اصلی بساز (الگوی _sm, _md, ...)
        var baseName = Path.GetFileNameWithoutExtension(img.Url);
        var thumbs = new[] { $"{baseName}_sm.webp", $"{baseName}_md.webp" }; // اگر سایزهای دیگری داری، اضافه کن
        await _proc.DeleteRelatedAsync(img.Url, thumbs, ct);

        // اگر MainImage حذف شد، جایگزین کن
        if (p.MainImageId == img.Id)
        {
            var next = await _db.Set<ProductImage>().AsNoTracking()
                .Where(i => i.ProductId == p.Id)
                .OrderBy(i => i.SortOrder)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync(ct);

            _db.Entry(p).Property(nameof(Product.MainImageId)).CurrentValue = next;
            await _db.SaveChangesAsync(ct);
        }

        return Unit.Value;
    }
}