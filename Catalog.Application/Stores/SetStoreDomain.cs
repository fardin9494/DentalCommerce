// SetStoreDomain.cs
using MediatR;

namespace Catalog.Application.Stores;
public sealed record SetStoreDomainCommand(Guid StoreId, string? Domain) : IRequest<Unit>;