// SetStoreDomainHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed class SetStoreDomainHandler : IRequestHandler<SetStoreDomainCommand, Unit>
{
    private readonly DbContext _db;
    public SetStoreDomainHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(SetStoreDomainCommand req, CancellationToken ct)
    {
        var s = await _db.Set<Store>().FirstOrDefaultAsync(x => x.Id == req.StoreId, ct)
                ?? throw new InvalidOperationException("Store not found.");

        var newDomain = string.IsNullOrWhiteSpace(req.Domain) ? null : req.Domain.Trim().ToLowerInvariant();
        _db.Entry(s).Property(nameof(Store.Domain)).CurrentValue = newDomain;

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}