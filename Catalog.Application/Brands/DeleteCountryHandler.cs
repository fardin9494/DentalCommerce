using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class DeleteCountryHandler : IRequestHandler<DeleteCountryCommand>
{
    private readonly DbContext _db;
    public DeleteCountryHandler(DbContext db) => _db = db;

    public async Task Handle(DeleteCountryCommand req, CancellationToken ct)
    {
        var code2 = req.Code2.ToUpperInvariant();
        var c = await _db.Set<Country>().FindAsync(new object?[] { code2 }, ct);
        if (c is null) return; // idempotent

        _db.Set<Country>().Remove(c);
        await _db.SaveChangesAsync(ct);
    }
}

