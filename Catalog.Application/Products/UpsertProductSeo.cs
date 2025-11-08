using MediatR;

namespace Catalog.Application.Products;

public sealed record UpsertProductSeoCommand(
    Guid ProductId,
    Guid StoreId,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    string? Robots,
    string? JsonLd
) : IRequest<Unit>;