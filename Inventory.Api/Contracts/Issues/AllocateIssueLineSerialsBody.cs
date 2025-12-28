namespace Inventory.Api.Contracts.Issues;

public sealed record AllocateIssueLineSerialsBody(IReadOnlyList<string> Serials);
