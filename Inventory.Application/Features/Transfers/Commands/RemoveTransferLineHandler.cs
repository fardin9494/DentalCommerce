using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class RemoveTransferLineHandler : IRequestHandler<RemoveTransferLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public RemoveTransferLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RemoveTransferLineCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");
        tr.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

