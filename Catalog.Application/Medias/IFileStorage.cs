namespace Catalog.Application.Medias;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream stream, string fileName, string contentType, CancellationToken ct);
    Task DeleteAsync(string storedPath, CancellationToken ct);
    string GetPublicUrl(string storedPath);
}