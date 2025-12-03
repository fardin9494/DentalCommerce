using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class UpdateTransferHeaderHandler : IRequestHandler<UpdateTransferHeaderCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public UpdateTransferHeaderHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateTransferHeaderCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers.FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");
        tr.UpdateHeader(req.ExternalRef, req.DocDateUtc);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

