namespace Catalog.Application.Medias;

public sealed class ProcessedImageResult
{
    public string OriginalPath { get; init; } = default!;
    public int Width { get; init; }
    public int Height { get; init; }
    public long SizeBytes { get; init; }
    public string ContentType { get; init; } = default!;
    public Dictionary<string, string> Thumbs { get; init; } = new();
}

public interface IImageProcessor
{
    Task<ProcessedImageResult> ProcessAndSaveAsync(Stream input, string originalFileName, CancellationToken ct);
    Task DeleteRelatedAsync(string originalPath, IEnumerable<string> thumbNames, CancellationToken ct);
}