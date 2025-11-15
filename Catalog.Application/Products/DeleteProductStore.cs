using MediatR;

namespace Catalog.Application.Products;

public sealed record DeleteProductStoreCommand(Guid ProductId, Guid StoreId) : IRequest<Unit>;

