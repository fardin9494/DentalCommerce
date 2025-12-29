using Inventory.Application.Common.Interfaces;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Features.Receipts.Commands;

public sealed class AddReceiptLineHandler : IRequestHandler<AddReceiptLineCommand, Guid>
{
    private readonly IInventoryDbContext _db;
    private readonly ICatalogGateway _catalogGateway;
    private readonly ITransactionRunner _tx;
    private readonly ILogger<AddReceiptLineHandler> _logger;

    public AddReceiptLineHandler(
        IInventoryDbContext db,
        ICatalogGateway catalogGateway,
        ITransactionRunner tx,
        ILogger<AddReceiptLineHandler> logger)
    {
        _db = db;
        _catalogGateway = catalogGateway;
        _tx = tx;
        _logger = logger;
    }

    public async Task<Guid> Handle(AddReceiptLineCommand req, CancellationToken ct)
    {
        const int maxAttempts = 3;

        DbUpdateConcurrencyException? lastEx = null;
        List<string> lastEntities = new();
        List<string> lastStates = new();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await _tx.ExecuteAsync(async ct =>
                {
                    var rec = await _db.Receipts
                        .Include(r => r.Lines)
                        .FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                        ?? throw new InvalidOperationException("رسید پیدا نشد.");

                    // Validate product and variant exist in Catalog before adding line
                    var catalogItem = await _catalogGateway.GetCatalogItemAsync(req.ProductId, req.VariantId, ct);
                    if (catalogItem is null)
                    {
                        var errorMessage = req.VariantId.HasValue
                            ? $"محصول با شناسه {req.ProductId} یا variant با شناسه {req.VariantId.Value} در کاتالوگ یافت نشد یا غیرفعال است."
                            : $"محصول با شناسه {req.ProductId} در کاتالوگ یافت نشد.";
                        throw new InvalidOperationException(errorMessage);
                    }

                    var line = rec.AddLine(req.ProductId, req.VariantId, req.Qty, req.LotNumber, req.ExpiryDateUtc, req.UnitCost);
                    _db.Entry(line).State = EntityState.Added;

                    await _db.SaveChangesAsync(ct);
                    return line.Id;
                }, ct);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                    lastEx = ex;

                    // تلاش مجدد پس از بارگذاری مجدد موجودیت‌هایی که در خطای همزمانی دخیل بوده‌اند
                    lastEntities = ex.Entries
                        .Select(e =>
                        {
                            var pkValues = e.Properties
                                .Where(p => p.Metadata.IsPrimaryKey())
                                .Select(p => $"{p.Metadata.Name}={p.CurrentValue}");
                            var pkText = string.Join(",", pkValues);
                            return $"{e.Metadata.Name}[{pkText}]";
                        })
                        .Distinct()
                        .ToList();
                    lastStates = ex.Entries
                        .Select(e => $"{e.Metadata.Name}:{e.State}")
                        .Distinct()
                        .ToList();

                    _logger.LogWarning(
                        "Concurrency attempt {Attempt} for ReceiptId {ReceiptId} -> Entities: {Entities} | States: {States}",
                        attempt,
                        req.ReceiptId,
                        string.Join(", ", lastEntities),
                        string.Join(", ", lastStates));

                    foreach (var entry in ex.Entries)
                    {
                        switch (entry.State)
                        {
                            case EntityState.Modified:
                            case EntityState.Unchanged:
                                await entry.ReloadAsync(ct);
                                break;
                            default:
                                entry.State = EntityState.Detached;
                                break;
                        }
                    }
                    _db.ChangeTracker.Clear(); // بازخوانی برای تلاش بعدی

                    if (attempt == maxAttempts)
                        break;
                }
            }

            var entities = lastEntities.Any() ? $" (Entities: {string.Join(", ", lastEntities)})" : " (Entities: none)";
            var states = lastStates.Any() ? $" (States: {string.Join(", ", lastStates)})" : " (States: none)";
            var tracked = _db.ChangeTracker.Entries()
                .Select(e =>
                {
                    var pkValues = e.Properties
                        .Where(p => p.Metadata.IsPrimaryKey())
                        .Select(p => $"{p.Metadata.Name}={p.CurrentValue}");
                    var pkText = string.Join(",", pkValues);
                    return $"{e.Entity.GetType().Name}:{e.State}[{pkText}]";
                });
            var trackedInfo = tracked.Any() ? $" (Tracked: {string.Join(" | ", tracked)})" : " (Tracked: none)";

            throw new InvalidOperationException(
                $"رکورد توسط کاربر دیگری تغییر یافته است. لطفا دوباره تلاش کنید.{entities}{states}{trackedInfo}",
                lastEx);
    }
}
