using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class UpdateAdjustmentHeaderHandler : IRequestHandler<UpdateAdjustmentHeaderCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateAdjustmentHeaderHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateAdjustmentHeaderCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments.FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("سند اصلاح پیدا نشد.");
        adj.UpdateHeader(req.Note, req.DocDateUtc);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

