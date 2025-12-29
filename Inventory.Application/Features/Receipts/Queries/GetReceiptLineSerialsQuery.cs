using Inventory.Domain.Enums;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Receipts.Queries;

public sealed record ReceiptLineSerialDto(
    Guid Id,
    string SerialNumber,
    string Status
);

public sealed record GetReceiptLineSerialsQuery(Guid ReceiptId, Guid LineId) : IRequest<IReadOnlyList<ReceiptLineSerialDto>>;

public sealed class GetReceiptLineSerialsHandler : IRequestHandler<GetReceiptLineSerialsQuery, IReadOnlyList<ReceiptLineSerialDto>>
{
    private readonly IInventoryDbContext _db;
    public GetReceiptLineSerialsHandler(IInventoryDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReceiptLineSerialDto>> Handle(GetReceiptLineSerialsQuery req, CancellationToken ct)
    {
        var exists = await _db.ReceiptLines.AnyAsync(l => l.Id == req.LineId && l.ReceiptId == req.ReceiptId, ct);
        if (!exists) throw new InvalidOperationException("Receipt line not found.");

        var serials = await _db.StockItemSerials
            .AsNoTracking()
            .Where(s => s.ReceiptLineId == req.LineId)
            .OrderBy(s => s.SerialNumber)
            .Select(s => new ReceiptLineSerialDto(
                s.Id,
                s.SerialNumber,
                Enum.IsDefined(typeof(StockSerialStatus), s.Status) ? s.Status.ToString() : StockSerialStatus.Draft.ToString()
            ))
            .ToListAsync(ct);

        return serials;
    }
}
