using MediatR;

namespace Inventory.Application.Features.Reservations.Commands;

public sealed record ReserveStockForOrderCommand(
    Guid OrderId,
    IReadOnlyList<ReserveStockLine> Lines) : IRequest<ReserveStockResult>;

public sealed record ReserveStockLine(
    string SkuId,
    decimal Qty);

public sealed record ReserveStockResult(
    Guid OrderId,
    IReadOnlyList<ReservedStockItem> Reserved);

public sealed record ReservedStockItem(
    Guid StockItemId,
    string SkuId,
    decimal Qty);

