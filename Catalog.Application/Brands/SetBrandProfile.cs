using MediatR;

namespace Catalog.Application.Brands;

public sealed record SetBrandProfileCommand(
    Guid BrandId,
    string? Description,
    int? EstablishedYear,
    Guid? LogoMediaId,
    string? Website
) : IRequest<Unit>;