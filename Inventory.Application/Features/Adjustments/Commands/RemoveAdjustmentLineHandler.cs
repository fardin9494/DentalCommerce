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
                  ?? throw new InvalidOperationException("سند اصلاح موجودی یافت نشد.");
        
        // Check if line exists before trying to remove
        var lineExists = adj.Lines.Any(l => l.Id == req.LineId);
        if (!lineExists)
        {
            throw new InvalidOperationException($"خط با شناسه {req.LineId} در سند اصلاح موجودی یافت نشد.");
        }
        
        adj.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

