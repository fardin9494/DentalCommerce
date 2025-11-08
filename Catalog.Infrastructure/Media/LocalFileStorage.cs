using Catalog.Application.Medias;
using Microsoft.Extensions.Configuration;

namespace Catalog.Infrastructure.Media;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly string _baseUrl;

    public LocalFileStorage(IConfiguration cfg)
    {
        _root = Path.GetFullPath(cfg["Media:Root"] ?? "./_media");
        _baseUrl = (cfg["Media:BaseUrl"] ?? "/media").TrimEnd('/');
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct)
    {
        var safeName = Path.GetFileName(fileName);
        var full = Path.Combine(_root, safeName);
        // اگر اسم از قبل وجود داشته باشد، یک GUID به ابتدای نام اضافه کن
        if (File.Exists(full))
        {
            var ext = Path.GetExtension(safeName);
            var name = Path.GetFileNameWithoutExtension(safeName);
            safeName = $"{Guid.NewGuid():N}-{name}{ext}";
            full = Path.Combine(_root, safeName);
        }

        await using var fs = new FileStream(full, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fs, ct);
        return safeName; // همان نام ذخیره‌شده
    }

    public async Task DeleteAsync(string storedPath, CancellationToken ct)
    {
        var full = Path.Combine(_root, storedPath);
        if (File.Exists(full)) File.Delete(full);
        await Task.CompletedTask;
    }

    public string GetPublicUrl(string storedPath) => $"{_baseUrl}/{storedPath}";
}