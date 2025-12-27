using Inventory.Domain.Enums;
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
        var rec = await _db.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("Receipt not found.");

        if (rec.Status != ReceiptStatus.Draft)
            throw new InvalidOperationException("Receipt line updates are only allowed in draft status.");

        var line = rec.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("Receipt line not found.");

        if (req.Qty.HasValue)
        {
            var serialCount = await _db.StockItemSerials.CountAsync(s => s.ReceiptLineId == line.Id, ct);
            if (serialCount > 0)
            {
                var qtyInt = EnsureWholeQty(req.Qty.Value);
                if (serialCount != qtyInt)
                    throw new InvalidOperationException("Serial count does not match the line quantity.");
            }

            line.UpdateQty(req.Qty.Value);
        }

        if (req.UnitCost.HasValue) line.UpdateUnitCost(req.UnitCost);
        line.UpdateLotNumber(req.LotNumber);
        line.UpdateExpiryDate(req.ExpiryDateUtc);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    private static int EnsureWholeQty(decimal qty)
    {
        var truncated = decimal.Truncate(qty);
        if (qty != truncated)
            throw new InvalidOperationException("Serialized lines require whole-number quantities.");
        return (int)truncated;
    }
}
