using MediatR;

namespace Catalog.Application.Brands;

public sealed record RemoveBrandAliasCommand(Guid AliasId) : IRequest<Unit>;