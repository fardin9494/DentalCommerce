using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed record GetBrandByIdQuery(Guid BrandId) : IRequest<BrandDto?>;

public sealed class BrandDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = default!;
    public string NormalizedName { get; init; } = default!;
    public string CountryCode { get; init; } = default!;
    public string? Website { get; init; }
    public string? Description { get; init; }
    public int? EstablishedYear { get; init; }
    public Guid? LogoMediaId { get; init; }
    public BrandStatus Status { get; init; }
}

public sealed class GetBrandByIdHandler : IRequestHandler<GetBrandByIdQuery, BrandDto?>
{
    private readonly DbContext _db;
    public GetBrandByIdHandler(DbContext db) => _db = db;

    public async Task<BrandDto?> Handle(GetBrandByIdQuery req, CancellationToken ct)
        => await _db.Set<Brand>().AsNoTracking()
            .Where(b => b.Id == req.BrandId)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                NormalizedName = b.NormalizedName,
                CountryCode = b.CountryCode,
                Website = b.Website,
                Description = b.Description,
                EstablishedYear = b.EstablishedYear,
                LogoMediaId = b.LogoMediaId,
                Status = b.Status
            })
            .FirstOrDefaultAsync(ct);
}