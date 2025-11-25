using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class CancelReceiptHandler : IRequestHandler<CancelReceiptCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public CancelReceiptHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CancelReceiptCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts.FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct);
        if (rec is null) throw new InvalidOperationException("رسید یافت نشد.");

        rec.Cancel();
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}