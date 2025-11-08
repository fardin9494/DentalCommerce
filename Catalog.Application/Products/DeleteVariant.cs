using MediatR;

namespace Catalog.Application.Products;

public sealed record DeleteVariantCommand(Guid ProductId, Guid VariantId) : IRequest<Unit>;