namespace Catalog.Application.Medias;

public sealed class ImageThumbOption
{
    public string Name { get; set; } = "sm";
    public int MaxEdge { get; set; } = 200;
    public int Quality { get; set; } = 80;
}

public sealed class ImageProcessingOptions
{
    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedFormats { get; set; } = new[] { "jpeg", "png", "webp" };
    public int MinWidth { get; set; } = 200;
    public int MinHeight { get; set; } = 200;
    public int MaxWidth { get; set; } = 4000;
    public int MaxHeight { get; set; } = 4000;
    public bool SaveOriginalAsWebp { get; set; } = true;
    public int OriginalQuality { get; set; } = 85;
    public List<ImageThumbOption> Thumbs { get; set; } = new();
}