using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed class SetBrandStatusHandler : IRequestHandler<SetBrandStatusCommand, Unit>
{
    private readonly DbContext _db;
    public SetBrandStatusHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(SetBrandStatusCommand req, CancellationToken ct)
    {
        var b = await _db.Set<Brand>().FirstOrDefaultAsync(x => x.Id == req.BrandId, ct);
        if (b is null) throw new InvalidOperationException("Brand not found.");

        b.SetStatus(req.Status);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}