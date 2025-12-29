using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;


public sealed class CancelAdjustmentHandler : IRequestHandler<CancelAdjustmentCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public CancelAdjustmentHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelAdjustmentCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments.FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("سند اصلاح موجودی یافت نشد.");

        adj.Cancel();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}