// CreateStoreHandler.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed class CreateStoreHandler : IRequestHandler<CreateStoreCommand, Guid>
{
    private readonly DbContext _db;
    public CreateStoreHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStoreCommand req, CancellationToken ct)
    {
        var domain = string.IsNullOrWhiteSpace(req.Domain) ? null : req.Domain.Trim().ToLowerInvariant();
        var store = Store.Create(req.Name, domain);
        _db.Set<Store>().Add(store);
        await _db.SaveChangesAsync(ct);
        return store.Id;
    }
}