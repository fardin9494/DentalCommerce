using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class UpdateAdjustmentLineHandler : IRequestHandler<UpdateAdjustmentLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateAdjustmentLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateAdjustmentLineCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments.Include(a => a.Lines).FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("سند اصلاح پیدا نشد.");
        var line = adj.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط سند اصلاح پیدا نشد.");
        line.UpdateQtyDelta(req.QtyDelta);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

