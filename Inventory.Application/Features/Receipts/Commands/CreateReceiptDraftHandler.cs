using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class CreateReceiptDraftHandler : IRequestHandler<CreateReceiptDraftCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateReceiptDraftHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateReceiptDraftCommand req, CancellationToken ct)
    {
        // وجود Warehouse را می‌توانی در همین کانتکست چک کنی:
        var existsWh = await _db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId, ct);
        if (!existsWh) throw new InvalidOperationException("انبار یافت نشد.");

        var rec = Receipt.Create(req.WarehouseId, req.DocDateUtc, req.ExternalRef);
        _db.Receipts.Add(rec);
        await _db.SaveChangesAsync(ct);
        return rec.Id;
    }
}