using Inventory.Domain.Aggregates;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class CreateReceiptDraftHandler : IRequestHandler<CreateReceiptDraftCommand, Guid>
{
    private readonly IInventoryDbContext _db;
    public CreateReceiptDraftHandler(IInventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateReceiptDraftCommand req, CancellationToken ct)
    {
        var existsWh = await _db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId, ct);
        if (!existsWh) throw new InvalidOperationException("انبار یافت نشد.");

        // ⬅️ امضای درست با Reason
        var docNo = await _db.NextReceiptDocNoAsync(ct);
        var rec = Receipt.Create(req.WarehouseId, req.Reason, docNo, req.DocDateUtc, req.ExternalRef);

        _db.Receipts.Add(rec);
        await _db.SaveChangesAsync(ct);
        return rec.Id;
    }
}
