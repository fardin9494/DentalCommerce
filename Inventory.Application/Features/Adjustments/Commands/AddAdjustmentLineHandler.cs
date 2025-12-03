using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;


public sealed class AddAdjustmentLineHandler : IRequestHandler<AddAdjustmentLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public AddAdjustmentLineHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(AddAdjustmentLineCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments
                      .Include(a => a.Lines)
                      .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("Adjustment پیدا نشد.");

        var line = adj.AddLine(req.ProductId, req.VariantId, req.LotNumber, req.ExpiryDateUtc, req.QtyDelta);
        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}