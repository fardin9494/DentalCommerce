using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Queries;

public sealed record GetShelvesListQuery(
    Guid? WarehouseId = null,
    bool? IsActive = null
) : IRequest<IReadOnlyList<ShelfListItemDto>>;

public sealed record ShelfListItemDto(
    Guid Id,
    Guid WarehouseId,
    string? WarehouseName,
    string Name,
    string Code,
    int RowNumber,
    int ColumnNumber,
    int LevelNumber,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed class GetShelvesListHandler : IRequestHandler<GetShelvesListQuery, IReadOnlyList<ShelfListItemDto>>
{
    private readonly InventoryDbContext _db;
    public GetShelvesListHandler(InventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<ShelfListItemDto>> Handle(GetShelvesListQuery req, CancellationToken ct)
    {
        var query = _db.StockShelves.AsNoTracking().AsQueryable();

        if (req.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == req.WarehouseId.Value);

        if (req.IsActive.HasValue)
            query = query.Where(s => s.IsActive == req.IsActive.Value);

        // Get warehouses for names
        var warehouseIds = await query.Select(s => s.WarehouseId).Distinct().ToListAsync(ct);
        var warehouses = await _db.Warehouses
            .AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.Name, ct);

        var shelves = await query
            .OrderBy(s => s.WarehouseId)
            .ThenBy(s => s.Name)
            .Select(s => new
            {
                s.Id,
                s.WarehouseId,
                s.Name,
                s.Code,
                s.RowNumber,
                s.ColumnNumber,
                s.LevelNumber,
                s.Description,
                s.IsActive,
                s.CreatedAt,
                s.UpdatedAt
            })
            .ToListAsync(ct);

        return shelves.Select(s => new ShelfListItemDto(
            s.Id,
            s.WarehouseId,
            warehouses.TryGetValue(s.WarehouseId, out var name) ? name : null,
            s.Name,
            s.Code,
            s.RowNumber,
            s.ColumnNumber,
            s.LevelNumber,
            s.Description,
            s.IsActive,
            s.CreatedAt,
            s.UpdatedAt
        )).ToList();
    }
}

