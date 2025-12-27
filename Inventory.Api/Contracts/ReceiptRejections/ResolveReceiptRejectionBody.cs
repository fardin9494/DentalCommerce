namespace Inventory.Api.Contracts.ReceiptRejections;

public sealed record ResolveReceiptRejectionBody(
    decimal ApprovedQty = 0,
    decimal ReturnedQty = 0,
    decimal DisposedQty = 0,
    string? Note = null
);
