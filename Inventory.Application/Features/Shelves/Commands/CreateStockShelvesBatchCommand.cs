using Inventory.Domain.Aggregates;
using Inventory.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Inventory.Application.Features.Shelves.Commands;

public sealed record CreateStockShelvesBatchCommand(
    Guid WarehouseId,
    int Rows,
    int Columns,
    int Levels = 1,
    string? Prefix = null,
    string? Description = null
) : IRequest<int>;

public sealed class CreateStockShelvesBatchHandler : IRequestHandler<CreateStockShelvesBatchCommand, int>
{
    private readonly InventoryDbContext _db;
    public CreateStockShelvesBatchHandler(InventoryDbContext db) => _db = db;

    public async Task<int> Handle(CreateStockShelvesBatchCommand req, CancellationToken ct)
    {
        if (req.Rows <= 0) throw new ArgumentOutOfRangeException(nameof(req.Rows), "تعداد ردیف باید بزرگتر از صفر باشد.");
        if (req.Columns <= 0) throw new ArgumentOutOfRangeException(nameof(req.Columns), "تعداد ستون/قفسه باید بزرگتر از صفر باشد.");
        if (req.Levels <= 0) throw new ArgumentOutOfRangeException(nameof(req.Levels), "تعداد طبقه باید بزرگتر از صفر باشد.");

        // سقف منطقی برای جلوگیری از انفجار داده
        const int maxDimension = 200;
        if (req.Rows > maxDimension || req.Columns > maxDimension || req.Levels > maxDimension)
            throw new InvalidOperationException("ابعاد وارد شده بسیار بزرگ است. لطفاً مقادیر معقول وارد کنید.");

        var warehouse = await _db.Warehouses.FirstOrDefaultAsync(w => w.Id == req.WarehouseId, ct)
            ?? throw new InvalidOperationException("انبار یافت نشد.");

        var prefix = PreparePrefix(req.Prefix, warehouse.Code);

        // پیش‌بارگذاری کدها/نام‌های موجود برای جلوگیری از برخورد
        var existing = await _db.StockShelves
            .Where(s => s.WarehouseId == req.WarehouseId)
            .Select(s => new { s.Name, s.Code })
            .ToListAsync(ct);

        var existingNames = existing.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var existingCodes = existing.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var shelves = new List<StockShelf>();
        for (var r = 1; r <= req.Rows; r++)
        {
            var rowLabel = ToAlphaLabel(r);
            for (var c = 1; c <= req.Columns; c++)
            {
                var colLabel = c.ToString("D2");
                for (var l = 1; l <= req.Levels; l++)
                {
                    var levelLabel = req.Levels > 1 ? $"-L{l}" : string.Empty;
                    var name = $"{rowLabel}{colLabel}{levelLabel}";
                    var code = $"{prefix}-{name}";

                    if (existingNames.Contains(name) || existingCodes.Contains(code))
                        continue; // از ایجاد تکراری صرفنظر می‌کنیم

                    var shelf = StockShelf.Create(
                        warehouseId: req.WarehouseId,
                        name: name,
                        code: code,
                        rowNumber: r,
                        columnNumber: c,
                        levelNumber: l,
                        description: req.Description
                    );
                    shelves.Add(shelf);
                    existingNames.Add(name);
                    existingCodes.Add(code);
                }
            }
        }

        if (shelves.Count == 0)
            throw new InvalidOperationException("هیچ قفسه جدیدی برای ایجاد وجود ندارد (همه کدها تکراری بودند).");

        await _db.StockShelves.AddRangeAsync(shelves, ct);
        await _db.SaveChangesAsync(ct);
        return shelves.Count;
    }

    private static string PreparePrefix(string? prefix, string warehouseCode)
    {
        var value = string.IsNullOrWhiteSpace(prefix) ? warehouseCode : prefix;
        var normalized = new StringBuilder();
        foreach (var ch in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '-')
                normalized.Append(ch);
        }
        var result = normalized.ToString();
        return string.IsNullOrWhiteSpace(result) ? warehouseCode.ToUpperInvariant() : result;
    }

    private static string ToAlphaLabel(int number)
    {
        // 1 -> A, 2 -> B, ..., 26 -> Z, 27 -> AA, ...
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var n = number;
        var sb = new StringBuilder();
        while (n > 0)
        {
            n--; // zero-based
            sb.Insert(0, alphabet[n % 26]);
            n /= 26;
        }
        return sb.ToString();
    }
}
