using Catalog.Application.Medias;
using Catalog.Domain.Brands;
using Catalog.Domain.Media;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed record UploadBrandLogoCommand(
    Guid BrandId,
    string FileName,
    string ContentType,
    Stream Content
) : IRequest<BrandLogoDto>;

public sealed record BrandLogoDto(Guid LogoMediaId, string LogoUrl);

public sealed class UploadBrandLogoHandler : IRequestHandler<UploadBrandLogoCommand, BrandLogoDto>
{
    private readonly DbContext _db;
    private readonly IImageProcessor _proc;

    public UploadBrandLogoHandler(DbContext db, IImageProcessor proc)
    {
        _db = db;
        _proc = proc;
    }

    public async Task<BrandLogoDto> Handle(UploadBrandLogoCommand req, CancellationToken ct)
    {
        var brand = await _db.Set<Brand>().FirstOrDefaultAsync(b => b.Id == req.BrandId, ct)
            ?? throw new InvalidOperationException("Brand not found.");

        var processed = await _proc.ProcessAndSaveAsync(req.Content, req.FileName, ct);
        var asset = MediaAsset.Create(processed.OriginalPath, processed.ContentType, processed.Thumbs);

        var assets = _db.Set<MediaAsset>();
        assets.Add(asset);

        MediaAsset? previous = null;
        if (brand.LogoMediaId.HasValue)
        {
            previous = await assets.FirstOrDefaultAsync(m => m.Id == brand.LogoMediaId.Value, ct);
            if (previous is not null)
            {
                assets.Remove(previous);
            }
        }

        brand.SetProfile(brand.Description, brand.EstablishedYear, asset.Id, brand.Website);

        await _db.SaveChangesAsync(ct);

        if (previous is not null)
        {
            await _proc.DeleteRelatedAsync(previous.StoredPath, previous.GetThumbFileNames(), ct);
        }

        return new BrandLogoDto(asset.Id, ToPublicPath(asset.StoredPath));
    }

    private static string ToPublicPath(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return string.Empty;
        var normalized = stored.Replace("\\", "/").TrimStart('/');
        return $"/media/{normalized}";
    }
}
