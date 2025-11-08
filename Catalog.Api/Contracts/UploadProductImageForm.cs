
namespace Catalog.Api.Contracts;

public sealed record UploadProductImageForm(IFormFile file, string? alt);
