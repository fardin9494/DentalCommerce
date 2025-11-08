using MediatR;

namespace Catalog.Application.Products;

public sealed record UpsertPropertyCommand(
    Guid ProductId,
    string Key,
    string? ValueString,
    decimal? ValueDecimal,
    bool? ValueBool,
    string? ValueJson
) : IRequest<Guid>;