using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Warehouses.Commands;

public sealed record CreateWarehouseCommand(string Code, string Name) : IRequest<Guid>;

public sealed class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateWarehouseHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateWarehouseCommand req, CancellationToken ct)
    {
        var code = req.Code.Trim().ToUpperInvariant();
        
        // Check for duplicate code
        var exists = await _db.Warehouses.AnyAsync(w => w.Code == code, ct);
        if (exists)
            throw new InvalidOperationException($"انبار با کد '{code}' قبلاً وجود دارد.");

        var warehouse = Warehouse.Create(req.Code, req.Name);
        _db.Warehouses.Add(warehouse);
        await _db.SaveChangesAsync(ct);
        
        return warehouse.Id;
    }
}

