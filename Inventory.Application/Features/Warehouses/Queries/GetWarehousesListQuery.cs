using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Warehouses.Queries;

public sealed record GetWarehousesListQuery(bool? IsActive = null) : IRequest<IReadOnlyList<WarehouseListItemDto>>;

public sealed record WarehouseListItemDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive
);

public sealed class GetWarehousesListHandler : IRequestHandler<GetWarehousesListQuery, IReadOnlyList<WarehouseListItemDto>>
{
    private readonly InventoryDbContext _db;
    public GetWarehousesListHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<WarehouseListItemDto>> Handle(GetWarehousesListQuery req, CancellationToken ct)
    {
        var query = _db.Warehouses.AsNoTracking().AsQueryable();

        if (req.IsActive.HasValue)
            query = query.Where(w => w.IsActive == req.IsActive.Value);

        return await query
            .OrderBy(w => w.Name)
            .Select(w => new WarehouseListItemDto(
                w.Id,
                w.Code,
                w.Name,
                w.IsActive
            ))
            .ToListAsync(ct);
    }
}

