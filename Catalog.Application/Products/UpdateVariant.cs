using MediatR;

namespace Catalog.Application.Products;

public sealed record UpdateVariantCommand(
    Guid ProductId,
    Guid VariantId,
    string VariantValue,
    string Sku,
    bool IsActive
) : IRequest<Unit>;

