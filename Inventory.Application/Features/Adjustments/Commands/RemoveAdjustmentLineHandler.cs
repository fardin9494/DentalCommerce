using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class RemoveAdjustmentLineHandler : IRequestHandler<RemoveAdjustmentLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public RemoveAdjustmentLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RemoveAdjustmentLineCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments.Include(a => a.Lines).FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("سند اصلاح پیدا نشد.");
        adj.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

