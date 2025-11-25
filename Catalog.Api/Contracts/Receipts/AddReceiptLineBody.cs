namespace Catalog.Api.Contracts.Receipts;

public sealed record AddReceiptLineBody(
    Guid ProductId,
    Guid? VariantId,
    decimal Qty,
    string? LotNumber,
    DateTime? ExpiryDateUtc,
    decimal? UnitCost
);