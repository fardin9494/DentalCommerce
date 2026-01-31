namespace Sales.Application.Abstractions;

public interface IInventoryReservationGateway
{
    Task ReserveAsync(Guid orderId, IReadOnlyList<InventoryReservationRequestLine> lines, CancellationToken ct);
    Task ReleaseAsync(Guid orderId, CancellationToken ct);
}

public sealed record InventoryReservationRequestLine(
    string SkuId,
    decimal Qty);
