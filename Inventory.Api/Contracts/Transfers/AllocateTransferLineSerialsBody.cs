namespace Inventory.Api.Contracts.Transfers;

public sealed class AllocateTransferLineSerialsBody
{
    public IReadOnlyList<string> Serials { get; set; } = Array.Empty<string>();
}
