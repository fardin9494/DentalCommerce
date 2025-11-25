using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class CreateTransferDraftHandler : IRequestHandler<CreateTransferDraftCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateTransferDraftHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateTransferDraftCommand req, CancellationToken ct)
    {
        var src = await _db.Warehouses.AnyAsync(w => w.Id == req.SourceWarehouseId, ct);
        var dst = await _db.Warehouses.AnyAsync(w => w.Id == req.DestinationWarehouseId, ct);
        if (!src || !dst) throw new InvalidOperationException("انبار مبدا/مقصد نامعتبر است.");

        var tr = Transfer.Create(req.SourceWarehouseId, req.DestinationWarehouseId, req.DocDateUtc, req.ExternalRef);
        _db.Transfers.Add(tr);
        await _db.SaveChangesAsync(ct);
        return tr.Id;
    }
}