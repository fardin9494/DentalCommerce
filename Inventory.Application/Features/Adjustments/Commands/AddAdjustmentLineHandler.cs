using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Adjustments.Commands;

public sealed class AddAdjustmentLineHandler : IRequestHandler<AddAdjustmentLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;

    public AddAdjustmentLineHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<Guid> Handle(AddAdjustmentLineCommand req, CancellationToken ct)
    {
        var adj = await _db.Adjustments
                      .Include(a => a.Lines)
                      .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("Adjustment پیدا نشد.");

        // Validate product and variant exist in Catalog before adding line
        var catalogItem = await _catalogGateway.GetCatalogItemAsync(req.ProductId, req.VariantId, ct);
        if (catalogItem is null)
        {
            var errorMessage = req.VariantId.HasValue
                ? $"محصول با شناسه {req.ProductId} یا variant با شناسه {req.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است."
                : $"محصول با شناسه {req.ProductId} در کاتالوگ یافت نشد.";
            throw new InvalidOperationException(errorMessage);
        }

        var line = adj.AddLine(req.ProductId, req.VariantId, req.LotNumber, req.ExpiryDateUtc, req.QtyDelta);
        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}