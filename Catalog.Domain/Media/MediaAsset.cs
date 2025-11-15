using System.Text.Json;
using BuildingBlocks.Domain;

namespace Catalog.Domain.Media;

public sealed class MediaAsset : AggregateRoot<Guid>
{
    public string StoredPath { get; private set; } = null!;
    public string ContentType { get; private set; } = null!;
    public string? ThumbsJson { get; private set; }

    private MediaAsset() { }

    public static MediaAsset Create(string storedPath, string contentType, IReadOnlyDictionary<string, string>? thumbs = null)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
            throw new ArgumentException("storedPath is required.", nameof(storedPath));

        if (string.IsNullOrWhiteSpace(contentType))
            contentType = "application/octet-stream";

        return new MediaAsset
        {
            Id = Guid.NewGuid(),
            StoredPath = storedPath,
            ContentType = contentType,
            ThumbsJson = SerializeThumbs(thumbs)
        };
    }

    public IReadOnlyCollection<string> GetThumbFileNames()
    {
        if (string.IsNullOrWhiteSpace(ThumbsJson))
            return Array.Empty<string>();

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(ThumbsJson);
            return dict?.Values?.ToArray() ?? Array.Empty<string>();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string? SerializeThumbs(IReadOnlyDictionary<string, string>? thumbs)
        => (thumbs is null || thumbs.Count == 0)
            ? null
            : JsonSerializer.Serialize(thumbs);
}
