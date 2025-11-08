using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Application.Medias;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record UploadProductImageCommand(
    Guid ProductId, string FileName, string ContentType, Stream Content, string? Alt = null
) : IRequest<Guid>;

public sealed class UploadProductImageHandler : IRequestHandler<UploadProductImageCommand, Guid>
{
    private readonly DbContext _db;
    private readonly IImageProcessor _proc;

    public UploadProductImageHandler(DbContext db, IImageProcessor proc)
    { _db = db; _proc = proc; }

    public async Task<Guid> Handle(UploadProductImageCommand req, CancellationToken ct)
    {
        var product = await _db.Set<Product>().FirstOrDefaultAsync(p => p.Id == req.ProductId, ct)
                      ?? throw new InvalidOperationException("Product not found.");

        var processed = await _proc.ProcessAndSaveAsync(req.Content, req.FileName, ct);

        var maxSort = await _db.Set<ProductImage>()
            .Where(i => i.ProductId == req.ProductId)
            .Select(i => (int?)i.SortOrder).MaxAsync(ct) ?? 0;

        var img = ProductImage.Create( // یا سازندهٔ موجود شما
            productId: req.ProductId,
            url: processed.OriginalPath,
            alt: string.IsNullOrWhiteSpace(req.Alt) ? null : req.Alt!.Trim(),
            sortOrder: maxSort + 1
        );

        _db.Set<ProductImage>().Add(img);
        await _db.SaveChangesAsync(ct);

        if (product.MainImageId is null)
        {
            _db.Entry(product).Property(nameof(Product.MainImageId)).CurrentValue = img.Id;
            await _db.SaveChangesAsync(ct);
        }

        return img.Id;
    }
}