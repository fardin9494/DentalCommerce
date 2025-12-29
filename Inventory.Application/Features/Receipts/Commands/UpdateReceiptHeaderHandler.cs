using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class UpdateReceiptHeaderHandler : IRequestHandler<UpdateReceiptHeaderCommand, Unit>
{
    private readonly IInventoryDbContext _db;
    public UpdateReceiptHeaderHandler(IInventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateReceiptHeaderCommand req, CancellationToken ct)
    {
        var rec = await _db.Receipts.FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("رسید پیدا نشد.");
        rec.UpdateHeader(req.ExternalRef, req.DocDateUtc);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

