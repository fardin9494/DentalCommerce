using Inventory.Domain.Aggregates;
using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed record SetReceiptLineSerialsCommand(
    Guid ReceiptId,
    Guid LineId,
    IReadOnlyList<string> Serials
) : IRequest<Unit>;

public sealed class SetReceiptLineSerialsHandler : IRequestHandler<SetReceiptLineSerialsCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public SetReceiptLineSerialsHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(SetReceiptLineSerialsCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("Receipt not found.");

        if (rec.Status != ReceiptStatus.Draft)
            throw new InvalidOperationException("Serials can only be edited while the receipt is in draft.");

        var line = rec.Lines.FirstOrDefault(l => l.Id == req.LineId)
                   ?? throw new InvalidOperationException("Receipt line not found.");

        var normalized = (req.Serials ?? Array.Empty<string>())
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList();

        var distinct = normalized
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinct.Count != normalized.Count)
            throw new InvalidOperationException("Serial numbers must be unique within the list.");

        if (distinct.Count > 0)
        {
            var qtyInt = EnsureWholeQty(line.Qty);
            if (distinct.Count != qtyInt)
                throw new InvalidOperationException("Serial count does not match the line quantity.");
        }

        if (distinct.Count > 0)
        {
            var existingSerials = await _db.StockItemSerials
                .AsNoTracking()
                .Where(s => distinct.Contains(s.SerialNumber) && s.ReceiptLineId != line.Id)
                .Select(s => s.SerialNumber)
                .ToListAsync(ct);

            if (existingSerials.Count > 0)
                throw new InvalidOperationException("Some serial numbers already exist in another receipt line.");
        }

        var existing = await _db.StockItemSerials
            .Where(s => s.ReceiptLineId == line.Id)
            .ToListAsync(ct);

        if (existing.Count > 0)
            _db.StockItemSerials.RemoveRange(existing);

        foreach (var serial in distinct)
        {
            var entity = StockItemSerial.Create(line.Id, serial);
            _db.StockItemSerials.Add(entity);
        }

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
