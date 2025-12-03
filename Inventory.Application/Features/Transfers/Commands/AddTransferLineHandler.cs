using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class AddTransferLineHandler : IRequestHandler<AddTransferLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;

    public AddTransferLineHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<Guid> Handle(AddTransferLineCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers.Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == req.TransferId, ct)
                 ?? throw new InvalidOperationException("سند انتقال پیدا نشد.");

        // Validate product and variant exist in Catalog before adding line
        var catalogItem = await _catalogGateway.GetCatalogItemAsync(req.ProductId, req.VariantId, ct);
        if (catalogItem is null)
        {
            var errorMessage = req.VariantId.HasValue
                ? $"محصول با شناسه {req.ProductId} یا variant با شناسه {req.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است."
                : $"محصول با شناسه {req.ProductId} در کاتالوگ یافت نشد.";
            throw new InvalidOperationException(errorMessage);
        }

        var line = tr.AddLine(req.ProductId, req.VariantId, req.Qty);
        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}