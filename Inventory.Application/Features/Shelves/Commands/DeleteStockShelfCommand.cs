using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public sealed record DeleteStockShelfCommand(Guid ShelfId) : IRequest<Unit>;

public sealed class DeleteStockShelfHandler : IRequestHandler<DeleteStockShelfCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public DeleteStockShelfHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteStockShelfCommand req, CancellationToken ct)
    {
        if (req.ShelfId == Guid.Empty) throw new ArgumentException("ShelfId required.", nameof(req.ShelfId));

        var shelf = await _db.StockShelves.FirstOrDefaultAsync(s => s.Id == req.ShelfId, ct)
                    ?? throw new InvalidOperationException("قفسه یافت نشد.");

        var hasAnyStock = await _db.StockItems.AnyAsync(si => si.ShelfId == req.ShelfId && si.OnHand > 0, ct);
        if (hasAnyStock)
            throw new InvalidOperationException("امکان حذف این قفسه به دلیل وجود محصول در آن وجود ندارد");

        _db.StockShelves.Remove(shelf);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

