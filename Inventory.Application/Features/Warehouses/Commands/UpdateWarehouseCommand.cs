using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Warehouses.Commands;

public sealed record UpdateWarehouseCommand(Guid Id, string Name) : IRequest<Unit>;

public sealed class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateWarehouseHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateWarehouseCommand req, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == req.Id, ct)
            ?? throw new InvalidOperationException("انبار یافت نشد.");

        warehouse.Rename(req.Name);
        await _db.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

