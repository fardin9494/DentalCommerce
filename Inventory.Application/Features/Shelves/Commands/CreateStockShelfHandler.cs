using BuildingBlocks.Domain;
using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public record CreateStockShelfCommand(Guid WarehouseId, string Name, string? Description) : IRequest<Guid>;

public class CreateStockShelfHandler : IRequestHandler<CreateStockShelfCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateStockShelfHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStockShelfCommand req, CancellationToken ct)
    {
        // بررسی تکراری نبودن نام شلف در آن انبار
        bool exists = await _db.StockShelves.AnyAsync(s =>
            s.WarehouseId == req.WarehouseId && s.Name == req.Name, ct);

        if (exists) throw new InvalidOperationException($"قفسه با نام {req.Name} در این انبار وجود دارد.");

        var shelf = StockShelf.Create(req.WarehouseId, req.Name, req.Description);
        _db.StockShelves.Add(shelf);
        await _db.SaveChangesAsync(ct);

        return shelf.Id;
    }
}