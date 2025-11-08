using MediatR;

namespace Catalog.Application.Products;

public sealed record SetVariationCommand(Guid ProductId, string? VariationKey) : IRequest<Unit>;