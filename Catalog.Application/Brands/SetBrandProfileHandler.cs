using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class SetBrandProfileHandler : IRequestHandler<SetBrandProfileCommand, Unit>
{
    private readonly DbContext _db;
    public SetBrandProfileHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(SetBrandProfileCommand req, CancellationToken ct)
    {
        var b = await _db.Set<Brand>().FirstOrDefaultAsync(x => x.Id == req.BrandId, ct);
        if (b is null) throw new InvalidOperationException("Brand not found.");

        b.SetProfile(req.Description, req.EstablishedYear, req.LogoMediaId, req.Website);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}