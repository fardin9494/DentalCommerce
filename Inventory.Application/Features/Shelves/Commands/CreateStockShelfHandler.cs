using BuildingBlocks.Domain;
using Inventory.Domain.Aggregates;
using Inventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Features.Shelves.Commands;

public record CreateStockShelfCommand(Guid WarehouseId, string Name, string? Description) : IRequest<Guid>;

public class CreateStockShelfHandler : IRequestHandler<CreateStockShelfCommand, Guid>
{
    private readonly IInventoryDbContext _db;
    public CreateStockShelfHandler(IInventoryDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateStockShelfCommand req, CancellationToken ct)
    {
        // بررسی تکراری نبودن نام شلف در آن انبار
        bool exists = await _db.StockShelves.AnyAsync(s =>
            s.WarehouseId == req.WarehouseId && s.Name == req.Name, ct);

        if (exists) throw new InvalidOperationException($"قفسه با نام {req.Name} در این انبار وجود دارد.");

        // کد پیشنهادی: بر اساس نام و حروف/اعداد مجاز
        var code = NormalizeCode(req.Name);

        var shelf = StockShelf.Create(
            warehouseId: req.WarehouseId,
            name: req.Name,
            code: code,
            rowNumber: 1,
            columnNumber: 1,
            levelNumber: 1,
            description: req.Description);
        _db.StockShelves.Add(shelf);
        await _db.SaveChangesAsync(ct);

        return shelf.Id;
    }

    private static string NormalizeCode(string name)
    {
        var chars = name.Trim().ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_')
            .ToArray();
        return chars.Length == 0 ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : new string(chars);
    }
}