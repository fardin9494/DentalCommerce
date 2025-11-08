using MediatR;

namespace Catalog.Application.Brands;
public sealed record DeleteBrandCommand(Guid BrandId) : IRequest<Unit>;