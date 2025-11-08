using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class AddProductImageHandler : IRequestHandler<AddProductImageCommand, Guid>
{
    private readonly DbContext _db;
    public AddProductImageHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(AddProductImageCommand req, CancellationToken ct)
    {
        var product = await _db.Set<Product>()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

        if (product is null)
            throw new InvalidOperationException("Product not found.");

        var img = ProductImage.Create(product.Id, req.Url, req.Alt, req.SortOrder ?? 0);

        product.Images.ToList();
       
        _db.Set<ProductImage>().Add(img);

        await _db.SaveChangesAsync(ct);

        if (req.MakeMain || product.MainImageId is null)
        {
            product.SetMainImage(img.Id);
            await _db.SaveChangesAsync(ct);
        }

        return img.Id;
    }
}