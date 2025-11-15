using Catalog.Domain.Brands;
using MediatR;

namespace Catalog.Application.Brands;

public sealed record CreateBrandCommand(
    string Name,
    string? Website,
    string? Description,
    int? EstablishedYear,
    Guid? LogoMediaId,
    BrandStatus? Status
) : IRequest<Guid>;
