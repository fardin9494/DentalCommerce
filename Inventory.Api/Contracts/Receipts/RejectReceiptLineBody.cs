namespace Inventory.Api.Contracts.Receipts;

public sealed record RejectReceiptLineBody(decimal Qty, string? Reason = null);

