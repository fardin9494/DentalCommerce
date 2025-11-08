using MediatR;

namespace Catalog.Application.Products;

public sealed record CreateProductCommand(
    string Name,
    string Slug,
    string Code,
    Guid BrandId,
    IReadOnlyList<Guid> CategoryIds,
    string? WarehouseCode,
    string? VariationKey
) : IRequest<Guid>;