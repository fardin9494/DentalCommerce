using Inventory.Domain.Enums;

namespace Inventory.Api.Contracts.Receipts;

public sealed record CreateReceiptBody(
    Guid WarehouseId,
    ReceiptReason Reason,
    string? ExternalRef,
    DateTime? DocDateUtc
);