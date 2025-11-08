using MediatR;

namespace Catalog.Application.Brands;

public sealed record CreateBrandCommand(
    string Name,
    string CountryCode,   // ISO2
    string? Website
) : IRequest<Guid>;