using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class AddReceiptLineHandler : IRequestHandler<AddReceiptLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public AddReceiptLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(AddReceiptLineCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts.Include(r => r.Lines).FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("رسید پیدا نشد.");

        var line = rec.AddLine(req.ProductId, req.VariantId, req.Qty, req.LotNumber, req.ExpiryDateUtc, req.UnitCost);

        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}