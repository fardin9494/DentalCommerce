using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Transfers.Commands;

public sealed class CompleteTransferHandler : IRequestHandler<CompleteTransferCommand, Unit>
{
    private readonly InventoryDbContext _db;
    public CompleteTransferHandler(InventoryDbContext db) => _db = db;

    public async Task<Unit> Handle(CompleteTransferCommand req, CancellationToken ct)
    {
        var tr = await _db.Transfers
            .Include(t => t.Lines)
            .ThenInclude(l => l.Segments)
            .FirstOrDefaultAsync(t => t.Id == req.TransferId, ct);

        if (tr is null)
            throw new InvalidOperationException("سند انتقال یافت نشد.");

        if (tr.Status != Domain.Enums.TransferStatus.PartiallyReceived)
            throw new InvalidOperationException(
                $"فقط انتقالی که تمام کالاهایش دریافت شده‌اند قابل تایید نهایی است. وضعیت فعلی: {tr.Status}"
            );

        // بررسی اینکه تمام segments دریافت شده‌اند
        var allReceived = tr.Lines.SelectMany(x => x.Segments).All(s => s.RemainingToReceive <= 0);
        if (!allReceived)
        {
            var remainingSegments = tr.Lines
                .SelectMany(x => x.Segments)
                .Where(s => s.RemainingToReceive > 0)
                .ToList();
            throw new InvalidOperationException(
                $"همه‌ی کالاها باید دریافت شوند قبل از تایید نهایی. " +
                $"تعداد segments باقی‌مانده: {remainingSegments.Count}, " +
                $"مجموع مقدار باقی‌مانده: {remainingSegments.Sum(s => s.RemainingToReceive)}"
            );
        }

        try
        {
            tr.Complete(req.WhenUtc);
            await _db.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"خطا در تایید نهایی سند انتقال {req.TransferId}: {ex.Message}",
                ex
            );
        }

        return Unit.Value;
    }
}

