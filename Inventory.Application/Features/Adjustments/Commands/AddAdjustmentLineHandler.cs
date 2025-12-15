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
        if (req.StockItemId == Guid.Empty)
            throw new InvalidOperationException("شناسه موجودی اجباری است.");

        var adj = await _db.Adjustments
                      .Include(a => a.Lines)
                      .FirstOrDefaultAsync(a => a.Id == req.AdjustmentId, ct)
                  ?? throw new InvalidOperationException("پیش‌نویس اصلاح موجودی پیدا نشد.");

        var stockItem = await _db.StockItems
            .AsNoTracking()
            .FirstOrDefaultAsync(si => si.Id == req.StockItemId, ct)
            ?? throw new InvalidOperationException("موجودی انتخاب شده یافت نشد.");

        if (stockItem.WarehouseId != adj.WarehouseId)
            throw new InvalidOperationException("موجودی انتخاب شده متعلق به انبار دیگری است.");

        if (stockItem.ProductId != req.ProductId || stockItem.VariantId != req.VariantId)
            throw new InvalidOperationException("موجودی انتخاب شده با محصول/تنوع درخواستی همخوانی ندارد.");

        // Validate product and variant exist in Catalog before adding line
        var catalogItem = await _catalogGateway.GetCatalogItemAsync(req.ProductId, req.VariantId, ct);
        if (catalogItem is null)
        {
            var errorMessage = req.VariantId.HasValue
                ? $"محصول با شناسه {req.ProductId} و variant با شناسه {req.VariantId.Value} در کاتالوگ پیدا نشد."
                : $"محصول با شناسه {req.ProductId} در کاتالوگ پیدا نشد.";
            throw new InvalidOperationException(errorMessage);
        }

        // Always bind the adjustment line to the exact StockItem that was picked in the UI
        var line = adj.AddLine(
            stockItem.Id,
            stockItem.ProductId,
            stockItem.VariantId,
            stockItem.LotNumber,
            stockItem.ExpiryDate,
            req.QtyDelta);

        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}
