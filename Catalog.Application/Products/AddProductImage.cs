using MediatR;

namespace Catalog.Application.Products;

public sealed record AddProductImageCommand(
    Guid ProductId,
    string Url,
    string? Alt,
    int? SortOrder,
    bool MakeMain
) : IRequest<Guid>;