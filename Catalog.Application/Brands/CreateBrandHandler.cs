using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class CreateBrandHandler : IRequestHandler<CreateBrandCommand, Guid>
{
    private readonly DbContext _db;
    public CreateBrandHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(CreateBrandCommand req, CancellationToken ct)
    {
        var b = Brand.Create(req.Name, req.CountryCode, req.Website);
        _db.Set<Brand>().Add(b);       // INSERT صریح
        await _db.SaveChangesAsync(ct);
        return b.Id;
    }
}