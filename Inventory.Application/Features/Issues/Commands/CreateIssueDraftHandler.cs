using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Issues.Commands;

public sealed class CreateIssueDraftHandler : IRequestHandler<CreateIssueDraftCommand, Guid>
{
    private readonly InventoryDbContext _db;
    public CreateIssueDraftHandler(InventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateIssueDraftCommand req, CancellationToken ct)
    {
        // اگر انبار مشخص شده باشد، بررسی می‌کنیم که وجود دارد
        if (req.WarehouseId.HasValue)
        {
            var existsWh = await _db.Warehouses.AnyAsync(w => w.Id == req.WarehouseId.Value, ct);
            if (!existsWh) throw new InvalidOperationException("انبار یافت نشد.");
        }

        var docNo = await _db.NextIssueDocNoAsync(ct);
        var issue = Issue.Create(docNo, req.WarehouseId, req.DocDateUtc, req.ExternalRef);
        _db.Issues.Add(issue);
        await _db.SaveChangesAsync(ct);
        return issue.Id;
    }
}
