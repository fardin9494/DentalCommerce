using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class RemoveReceiptLineHandler : IRequestHandler<RemoveReceiptLineCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public RemoveReceiptLineHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(RemoveReceiptLineCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("رسید پیدا نشد.");
        rec.RemoveLine(req.LineId);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
