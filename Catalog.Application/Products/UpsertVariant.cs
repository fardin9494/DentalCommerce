using MediatR;

namespace Catalog.Application.Products;

public sealed record UpsertVariantCommand(
    Guid ProductId,
    string VariantValue,   
    string Sku,           
    bool IsActive          
) : IRequest<Guid>;