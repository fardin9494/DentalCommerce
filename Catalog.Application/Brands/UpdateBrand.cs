using Catalog.Domain.Brands;
using MediatR;

namespace Catalog.Application.Brands;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string Name,
    string? Website,
    string? Description,
    int? EstablishedYear,
    BrandStatus Status
) : IRequest<Unit>;
