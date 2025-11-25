using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class AddTransferLineHandler : IRequestHandler<AddTransferLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public AddTransferLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(AddTransferLineCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        var line = tr.AddLine(req.ProductId, req.VariantId, req.Qty);
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}