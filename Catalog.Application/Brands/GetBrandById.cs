using Catalog.Domain.Brands;
using Catalog.Domain.Media;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed record GetBrandByIdQuery(Guid BrandId) : IRequest<BrandDto?>;

public sealed class BrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string NormalizedName { get; init; } = default!;
    public string? Website { get; init; }
    public string? Description { get; init; }
    public int? EstablishedYear { get; init; }
    public Guid? LogoMediaId { get; init; }
    public string? LogoUrl { get; init; }
    public BrandStatus Status { get; init; }
}

public sealed class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, BrandDto?>
{
    private readonly DbContext _db;
    public GetBrandByIdHandler(DbContext db) => _db = db;

    public async Task<BrandDto?> Handle(GetBrandByIdQuery req, CancellationToken ct)
    {
        var brands = _db.Set<Brand>().AsNoTracking();
        var media = _db.Set<MediaAsset>().AsNoTracking();

        return await brands
            .Where(b => b.Id == req.BrandId)
            .GroupJoin(media, b => b.LogoMediaId, m => m.Id, (b, logos) => new { Brand = b, logos })
            .SelectMany(x => x.logos.DefaultIfEmpty(), (x, logo) => new BrandDto
            {
                Id = x.Brand.Id,
                Name = x.Brand.Name,
                NormalizedName = x.Brand.NormalizedName,
                Website = x.Brand.Website,
                Description = x.Brand.Description,
                EstablishedYear = x.Brand.EstablishedYear,
                LogoMediaId = x.Brand.LogoMediaId,
                LogoUrl = logo != null ? ToPublicUrl(logo.StoredPath) : null,
                Status = x.Brand.Status
            })
            .FirstOrDefaultAsync(ct);
    }

    private static string ToPublicUrl(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return string.Empty;
        var normalized = storedPath.Replace("\\", "/").TrimStart('/');
        return $"/media/{normalized}";
    }
}

