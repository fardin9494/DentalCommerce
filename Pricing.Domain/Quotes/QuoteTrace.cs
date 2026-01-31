namespace Pricing.Domain.Quotes;

public sealed record QuoteTraceEntry(string Stage, string Message);

public sealed record QuoteSourceResult(
    string SourceType,
    string SourceId,
    string Name,
    bool Applied,
    string? Reason,
    int? Priority,
    string? StackingGroup);
