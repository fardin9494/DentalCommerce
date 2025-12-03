using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class UpdateTransferLineHandler : IRequestHandler<UpdateTransferLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateTransferLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateTransferLineCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");
        var line = tr.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند انتقال پیدا نشد.");
        line.UpdateQty(req.Qty);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

