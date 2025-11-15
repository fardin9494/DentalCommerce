using MediatR;

namespace Catalog.Application.Products;

public sealed record SetProductDescriptionCommand(Guid ProductId, string? ContentHtml) : IRequest<Unit>;