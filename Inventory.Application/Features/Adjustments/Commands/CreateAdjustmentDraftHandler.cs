namespace Inventory.Application.Features.Adjustments.Commands;

using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;

public sealed class CreateAdjustmentDraftHandler : IRequestHandler<CreateAdjustmentDraftCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateAdjustmentDraftHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAdjustmentDraftCommand req, CancellationToken ct)
    {
        var adj = Adjustment.Create(req.WarehouseId, req.Reason, req.DocDateUtc, req.Note);
        _db.Adjustments.Add(adj);
        await _db.SaveChangesAsync(ct);
        return adj.Id;
    }
}