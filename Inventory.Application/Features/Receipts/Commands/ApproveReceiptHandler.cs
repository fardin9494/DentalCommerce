using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class ApproveReceiptCommand : IRequest<Unit>
{
    public Guid ReceiptId { get; set; }
}

public sealed class ApproveReceiptHandler : IRequestHandler<ApproveReceiptCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public ApproveReceiptHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(ApproveReceiptCommand req, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var rec = await _db.Receipts.Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct);

        if (rec is null) throw new InvalidOperationException("رسید یافت نشد.");

        // 1. تغییر وضعیت سند
        rec.Approve(); // Status -> Approved

        // 2. آزادسازی موجودی (Unblock)
        foreach (var l in rec.Lines)
        {
            var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                si.ProductId == l.ProductId &&
                si.VariantId == l.VariantId &&
                si.WarehouseId == rec.WarehouseId &&
                si.LotNumber == l.LotNumber &&
                si.ExpiryDate == l.ExpiryDate, ct);

            if (stock is null) throw new InvalidOperationException($"رکورد موجودی برای خط {l.LineNo} یافت نشد.");

            // کالا را از قرنطینه آزاد می‌کنیم -> به Available اضافه می‌شود
            stock.Unblock(l.Qty);
        }

        // نکته: اینجا معمولاً نیازی به درج در StockLedger نیست چون موجودی کل (OnHand) تغییر نکرده،
        // فقط وضعیت کیفی آن تغییر کرده است. اما اگر بخواهید می‌توانید یک لاگ جداگانه بگیرید.

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Unit.Value;
    }
}