namespace Inventory.Api.Contracts.Receipts;

public sealed record SetReceiptLineSerialsBody(
    IReadOnlyList<string> Serials
);
