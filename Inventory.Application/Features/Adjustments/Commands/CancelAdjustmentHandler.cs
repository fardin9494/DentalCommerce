using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;


public sealed class CancelAdjustmentHandler : IRequestHandler<CancelAdjustmentCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public CancelAdjustmentHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelAdjustmentCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments.FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("Adjustment پیدا نشد.");

        adj.Cancel();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}