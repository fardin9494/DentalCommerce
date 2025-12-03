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
        var strategy = _db.Database.CreateExecutionStrategy();
        const int maxAttempts = 5;

        await strategy.ExecuteAsync(async () =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    var rec = await _db.Receipts.Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    rec.Approve();

                    foreach (var l in rec.Lines)
                    {
                        var stock = await _db.StockItems.FirstOrDefaultAsync(si =>
                            si.ProductId == l.ProductId &&
                            si.VariantId == l.VariantId &&
                            si.WarehouseId == rec.WarehouseId &&
                            si.LotNumber == l.LotNumber &&
                            si.ExpiryDate == l.ExpiryDate, ct)
                            ?? throw new InvalidOperationException($"موجودی مربوط به خط {l.LineNo} یافت نشد.");

                        stock.Unblock(l.Qty);
                    }

                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    break;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
                {
                    await tx.RollbackAsync(ct);
                    _db.ChangeTracker.Clear();
                }
            }
        });

        return Unit.Value;
    }
}