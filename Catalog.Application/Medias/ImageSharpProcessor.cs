
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Catalog.Application.Medias;

public sealed class ImageSharpProcessor : IImageProcessor
{
    private readonly IFileStorage _fs;
    private readonly ImageProcessingOptions _opt;

    public ImageSharpProcessor(IFileStorage fs, IOptions<ImageProcessingOptions> options)
    {
        _fs = fs;
        _opt = options.Value;
    }

    public async Task<ProcessedImageResult> ProcessAndSaveAsync(Stream input, string originalFileName, CancellationToken ct)
    {
        // کپی به حافظه برای اندازه و پردازش
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, ct);
        if (ms.Length > _opt.MaxUploadBytes)
            throw new InvalidOperationException($"حجم فایل بیشتر از حد مجاز است ({_opt.MaxUploadBytes} bytes).");
        ms.Position = 0;

        // Load with format
        using var img = await Image.LoadAsync(ms, ct); // بدون out
        var fmt = img.Metadata.DecodedImageFormat;     // فرمتِ واقعیِ decode شده
        var formatKey = fmt?.Name?.ToLowerInvariant() ?? "unknown";
        if (!_opt.AllowedFormats.Contains(formatKey))
            throw new InvalidOperationException($"فرمت مجاز نیست. ({string.Join(", ", _opt.AllowedFormats)})");

        // Auto-orient و پاکسازی EXIF
        img.Mutate(x => x.AutoOrient());
        img.Metadata.ExifProfile = new ExifProfile();

        // حداقل/حداکثر ابعاد
        if (img.Width < _opt.MinWidth || img.Height < _opt.MinHeight)
            throw new InvalidOperationException($"ابعاد خیلی کوچک است. حداقل: {_opt.MinWidth}x{_opt.MinHeight}");

        if (img.Width > _opt.MaxWidth || img.Height > _opt.MaxHeight)
        {
            var ratio = Math.Min((double)_opt.MaxWidth / img.Width, (double)_opt.MaxHeight / img.Height);
            var w = (int)Math.Round(img.Width * ratio);
            var h = (int)Math.Round(img.Height * ratio);
            img.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(w, h), Mode = ResizeMode.Max, Sampler = KnownResamplers.Lanczos3 }));
        }

        // ذخیره فایل اصلی با فرمت هدف
        var originalExt = _opt.SaveOriginalAsWebp ? ".webp"
                       : formatKey == "jpeg" ? ".jpg"
                       : formatKey == "png" ? ".png"
                       : ".webp";

        var originalName = $"{Guid.NewGuid():N}{originalExt}";
        await using (var outStream = new MemoryStream())
        {
            if (_opt.SaveOriginalAsWebp || formatKey == "webp")
            {
                var enc = new WebpEncoder { Quality = _opt.OriginalQuality, FileFormat = WebpFileFormatType.Lossy };
                await img.SaveAsync(outStream, enc, ct);
            }
            else if (formatKey == "jpeg")
            {
                var enc = new JpegEncoder { Quality = _opt.OriginalQuality };
                await img.SaveAsync(outStream, enc, ct);
            }
            else // png
            {
                var enc = new PngEncoder { CompressionLevel = PngCompressionLevel.Level6 };
                await img.SaveAsync(outStream, enc, ct);
            }

            outStream.Position = 0;
            // ذخیره از طریق استوریج
            await _fs.SaveAsync(outStream, originalName, _opt.SaveOriginalAsWebp ? "image/webp" : (fmt?.DefaultMimeType ?? "application/octet-stream"), ct);
        }

        // ساخت thumbnail ها
        var thumbs = new Dictionary<string, string>();
        foreach (var t in _opt.Thumbs)
        {
            using var clone = img.CloneAs<Rgba32>();
            var maxEdge = Math.Max(clone.Width, clone.Height);
            if (maxEdge > t.MaxEdge)
            {
                var ratio = (double)t.MaxEdge / maxEdge;
                var w = (int)Math.Round(clone.Width * ratio);
                var h = (int)Math.Round(clone.Height * ratio);
                clone.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(w, h),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));
            }

            var thumbName = $"{Path.GetFileNameWithoutExtension(originalName)}_{t.Name}.webp";
            await using var tf = new MemoryStream();
            var enc = new WebpEncoder { Quality = t.Quality, FileFormat = WebpFileFormatType.Lossy };
            await clone.SaveAsync(tf, enc, ct);
            tf.Position = 0;
            await _fs.SaveAsync(tf, thumbName, "image/webp", ct);
            thumbs[t.Name] = thumbName;
        }

        // اندازه فایل اصلی که ذخیره شد را نمی‌دانیم؛ مشکلی نیست، اختیاری است:
        return new ProcessedImageResult
        {
            OriginalPath = originalName,
            Width = img.Width,
            Height = img.Height,
            SizeBytes = ms.Length, // اندازه ورودی؛ اگر خواستی سایز خروجی را هم نگه داری، LocalFileStorage می‌تواند برگرداند
            ContentType = _opt.SaveOriginalAsWebp
                ? "image/webp"
                : (fmt?.DefaultMimeType ?? "application/octet-stream"),
            Thumbs = thumbs
        };
    }

    public async Task DeleteRelatedAsync(string originalPath, IEnumerable<string> thumbNames, CancellationToken ct)
    {
        await _fs.DeleteAsync(originalPath, ct);
        foreach (var tn in thumbNames)
            await _fs.DeleteAsync(tn, ct);
    }
}
