using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public sealed record UpdateStockShelfCommand(Guid Id, string Name, string? Description) : IRequest<Unit>;

public sealed class UpdateStockShelfHandler : IRequestHandler<UpdateStockShelfCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateStockShelfHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateStockShelfCommand req, CancellationToken ct)
    {
        var shelf = await _db.StockShelves.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new InvalidOperationException("قفسه یافت نشد.");

        // Check for duplicate name in the same warehouse
        var exists = await _db.StockShelves.AnyAsync(
            s => s.WarehouseId == shelf.WarehouseId && s.Name == req.Name && s.Id != req.Id, ct);
        
        if (exists)
            throw new InvalidOperationException($"قفسه با نام '{req.Name}' در این انبار وجود دارد.");

        shelf.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

