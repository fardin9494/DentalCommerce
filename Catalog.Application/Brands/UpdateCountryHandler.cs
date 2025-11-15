using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class UpdateCountryHandler : IRequestHandler<UpdateCountryCommand>
{
    private readonly DbContext _db;
    public UpdateCountryHandler(DbContext db) => _db = db;

    public async Task Handle(UpdateCountryCommand req, CancellationToken ct)
    {
        var code2 = req.Code2.ToUpperInvariant();
        var c = await _db.Set<Country>().FindAsync(new object?[] { code2 }, ct);
        if (c is null) throw new InvalidOperationException("Country not found.");

        c.UpdateProfile(req.NameFa, req.NameEn, req.Region, req.FlagEmoji);
        await _db.SaveChangesAsync(ct);
    }
}

