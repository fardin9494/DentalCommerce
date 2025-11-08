using MediatR;

namespace Catalog.Application.Products;

public sealed record UpsertProductStoreCommand(
    Guid ProductId,
    Guid StoreId,
    bool IsVisible,
    string Slug,
    string? TitleOverride,
    string? DescriptionOverride
) : IRequest<Unit>;