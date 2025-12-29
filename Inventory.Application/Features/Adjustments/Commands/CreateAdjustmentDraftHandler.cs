namespace Inventory.Application.Features.Adjustments.Commands;

using Inventory.Domain.Aggregates;
using Inventory.Application.Abstractions;
using MediatR;

public sealed class CreateAdjustmentDraftHandler : IRequestHandler<CreateAdjustmentDraftCommand, Guid>
{
    private readonly IInventoryDbContext _db;
    public CreateAdjustmentDraftHandler(IInventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAdjustmentDraftCommand req, CancellationToken ct)
    {
        var docNo = await _db.NextAdjustmentDocNoAsync(ct);
        var adj = Adjustment.Create(req.WarehouseId, req.Reason, docNo, req.DocDateUtc, req.Note);
        _db.Adjustments.Add(adj);
        await _db.SaveChangesAsync(ct);
        return adj.Id;
    }
}
