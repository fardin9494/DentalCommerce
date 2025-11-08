using MediatR;

namespace Catalog.Application.Brands;

public sealed record AddBrandAliasCommand(Guid BrandId, string Alias, string? Locale) : IRequest<Guid>;