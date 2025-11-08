// GetStoreById.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Stores;

namespace Catalog.Application.Stores;

public sealed record GetStoreByIdQuery(Guid StoreId) : IRequest<StoreListItemDto?>;

public sealed class GetStoreByIdHandler : IRequestHandler<GetStoreByIdQuery, StoreListItemDto?>
{
    private readonly DbContext _db;
    public GetStoreByIdHandler(DbContext db) => _db = db;

    public async Task<StoreListItemDto?> Handle(GetStoreByIdQuery req, CancellationToken ct)
        => await _db.Set<Store>().AsNoTracking()
            .Where(s => s.Id == req.StoreId)
            .Select(x => new StoreListItemDto { Id = x.Id, Name = x.Name, Domain = x.Domain })
            .FirstOrDefaultAsync(ct);
}