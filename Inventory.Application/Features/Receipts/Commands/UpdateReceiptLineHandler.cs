using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class UpdateReceiptLineHandler : IRequestHandler<UpdateReceiptLineCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateReceiptLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateReceiptLineCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("رسید پیدا نشد.");
        var line = rec.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("خط رسید پیدا نشد.");
        if (req.Qty.HasValue) line.UpdateQty(req.Qty.Value);
        if (req.UnitCost.HasValue) line.UpdateUnitCost(req.UnitCost);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

