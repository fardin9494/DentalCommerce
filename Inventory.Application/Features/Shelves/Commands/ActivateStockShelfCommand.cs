using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public sealed record ActivateStockShelfCommand(Guid Id) : IRequest<Unit>;

public sealed class ActivateStockShelfHandler : IRequestHandler<ActivateStockShelfCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ActivateStockShelfHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ActivateStockShelfCommand req, CancellationToken ct)
    {
        var shelf = await _db.StockShelves.FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new InvalidOperationException("قفسه یافت نشد.");

        shelf.Activate();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

