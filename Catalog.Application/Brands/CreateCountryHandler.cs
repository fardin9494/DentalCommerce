using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class CreateCountryHandler : IRequestHandler<CreateCountryCommand, string>
{
    private readonly DbContext _db;
    public CreateCountryHandler(DbContext db) => _db = db;

    public async Task<string> Handle(CreateCountryCommand req, CancellationToken ct)
    {
        var c = Country.Create(req.Code2, req.Code3, req.NameFa, req.NameEn, req.Region, req.FlagEmoji);
        _db.Set<Country>().Add(c);     // INSERT صریح
        await _db.SaveChangesAsync(ct);
        return c.Code2;                // کلید
    }
}