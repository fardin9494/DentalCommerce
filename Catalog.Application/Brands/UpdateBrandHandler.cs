using Catalog.Domain.Brands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Brands;

public sealed class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, Unit>
{
    private readonly DbContext _db;

    public UpdateBrandHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateBrandCommand req, CancellationToken ct)
    {
        var brand = await _db.Set<Brand>().FirstOrDefaultAsync(b => b.Id == req.BrandId, ct)
            ?? throw new InvalidOperationException("Brand not found.");

        if (!string.Equals(brand.Name, req.Name, StringComparison.Ordinal))
        {
            brand.Rename(req.Name);
        }

        brand.SetProfile(req.Description, req.EstablishedYear, brand.LogoMediaId, req.Website);
        brand.SetStatus(req.Status);

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
