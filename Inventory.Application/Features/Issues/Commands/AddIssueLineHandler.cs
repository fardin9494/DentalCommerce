using Inventory.Application.Common.Interfaces;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class AddIssueLineHandler : IRequestHandler<AddIssueLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;

    public AddIssueLineHandler(InventoryDbContext db, ICatalogGateway catalogGateway)
    {
        _db = db;
        _catalogGateway = catalogGateway;
    }

    public async Task<Guid> Handle(AddIssueLineCommand req, CancellationToken ct)
    {
        var issue = await _db.Issues.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == req.IssueId, ct)
                    ?? throw new InvalidOperationException("سند خروج پیدا نشد.");

        // Validate product and variant exist in Catalog before adding line
        var catalogItem = await _catalogGateway.GetCatalogItemAsync(req.ProductId, req.VariantId, ct);
        if (catalogItem is null)
        {
            var errorMessage = req.VariantId.HasValue
                ? $"محصول با شناسه {req.ProductId} یا variant با شناسه {req.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است."
                : $"محصول با شناسه {req.ProductId} در کاتالوگ یافت نشد.";
            throw new InvalidOperationException(errorMessage);
        }

        var line = issue.AddLine(req.ProductId, req.VariantId, req.Qty);
        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}