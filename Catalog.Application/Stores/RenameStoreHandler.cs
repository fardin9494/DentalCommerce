// RenameStoreHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed class RenameStoreHandler : IRequestHandler<RenameStoreCommand, Unit>
{
    private readonly DbContext _db;
    public RenameStoreHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(RenameStoreCommand req, CancellationToken ct)
    {
        var s = await _db.Set<Store>().FirstOrDefaultAsync(x => x.Id == req.StoreId, ct)
                ?? throw new InvalidOperationException("Store not found.");

        // چون متد دامین نداری، مستقیم ست می‌کنیم (ساده و بی‌حاشیه)
        var nameProp = _db.Entry(s).Property(nameof(Store.Name));
        nameProp.CurrentValue = req.Name.Trim();

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}