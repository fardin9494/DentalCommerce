using MediatR;

namespace Catalog.Application.Products;

public sealed record HideProductCommand(Guid ProductId) : IRequest<Unit>;

