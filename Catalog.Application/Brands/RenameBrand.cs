using MediatR;

namespace Catalog.Application.Brands;

public sealed record RenameBrandCommand(Guid BrandId, string Name) : IRequest<Unit>;