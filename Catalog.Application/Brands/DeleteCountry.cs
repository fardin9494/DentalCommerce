using MediatR;

namespace Catalog.Application.Brands;

public sealed record DeleteCountryCommand(string Code2) : IRequest;

