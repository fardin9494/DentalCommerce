using MediatR;

namespace Catalog.Application.Products;

public sealed record ActivateProductCommand(Guid ProductId) : IRequest<Unit>;
