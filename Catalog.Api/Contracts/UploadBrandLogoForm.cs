using Microsoft.AspNetCore.Http;

namespace Catalog.Api.Contracts;

public sealed record UploadBrandLogoForm(IFormFile file);
