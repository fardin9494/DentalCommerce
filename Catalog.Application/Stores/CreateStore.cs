using MediatR;

namespace Catalog.Application.Stores;
public sealed record CreateStoreCommand(string Name, string? Domain) : IRequest<Guid>;