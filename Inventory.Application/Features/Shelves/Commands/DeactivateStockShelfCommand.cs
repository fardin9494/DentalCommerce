using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public sealed record DeactivateStockShelfCommand(Guid Id) : IRequest<Unit>;

public sealed class DeactivateStockShelfHandler : IRequestHandler<DeactivateStockShelfCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public DeactivateStockShelfHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(DeactivateStockShelfCommand req, CancellationToken ct)
    {
        var shelf = await _db.StockShelves.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new InvalidOperationException("قفسه یافت نشد.");

        shelf.Deactivate();
        await _db.SaveChangesAsync(ct);
        
        return Unit.Value;
    }
}

