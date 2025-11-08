using MediatR;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed record SetBrandStatusCommand(Guid BrandId, BrandStatus Status) : IRequest<Unit>;