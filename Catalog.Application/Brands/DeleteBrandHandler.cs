using Catalog.Application.Medias;
using Catalog.Domain.Brands;
using Catalog.Domain.Media;
using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, Unit>
{
    private readonly DbContext _db;
    private readonly IImageProcessor _proc;

    public DeleteBrandHandler(DbContext db, IImageProcessor proc)
    {
        _db = db;
        _proc = proc;
    }

    public async Task<Unit> Handle(DeleteBrandCommand req, CancellationToken ct)
    {
        var inUse = await _db.Set<Product>().AsNoTracking().AnyAsync(p => p.BrandId == req.BrandId, ct);
        if (inUse) throw new InvalidOperationException("امکان حذف وجود ندارد؛ این برند در برخی محصولات استفاده شده است.");

        var brand = await _db.Set<Brand>().FirstOrDefaultAsync(x => x.Id == req.BrandId, ct);
        if (brand is null) return Unit.Value;

        MediaAsset? logo = null;
        if (brand.LogoMediaId.HasValue)
        {
            logo = await _db.Set<MediaAsset>().FirstOrDefaultAsync(m => m.Id == brand.LogoMediaId.Value, ct);
            if (logo is not null)
            {
                _db.Set<MediaAsset>().Remove(logo);
            }
        }

        _db.Set<Brand>().Remove(brand);
        await _db.SaveChangesAsync(ct);

        if (logo is not null)
        {
            await _proc.DeleteRelatedAsync(logo.StoredPath, logo.GetThumbFileNames(), ct);
        }

        return Unit.Value;
    }
}
