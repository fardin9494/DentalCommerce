namespace Inventory.Api.Contracts.Reservations;

public sealed record ReserveStockForOrderBody(
    Guid OrderId,
    List<ReserveStockLineBody> Lines);

public sealed record ReserveStockLineBody(
    string SkuId,
    decimal Qty);

