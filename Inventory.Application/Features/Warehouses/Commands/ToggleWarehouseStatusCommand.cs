using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Warehouses.Commands;

public sealed record ActivateWarehouseCommand(Guid Id) : IRequest<Unit>;
public sealed record DeactivateWarehouseCommand(Guid Id) : IRequest<Unit>;

public sealed class ActivateWarehouseHandler : IRequestHandler<ActivateWarehouseCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public ActivateWarehouseHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ActivateWarehouseCommand req, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == req.Id, ct)
            ?? throw new InvalidOperationException("انبار یافت نشد.");

        warehouse.Activate();
        await _db.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

public sealed class DeactivateWarehouseHandler : IRequestHandler<DeactivateWarehouseCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public DeactivateWarehouseHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(DeactivateWarehouseCommand req, CancellationToken ct)
    {
        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == req.Id, ct)
            ?? throw new InvalidOperationException("انبار یافت نشد.");

        warehouse.Deactivate();
        await _db.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

