// RenameStore.cs
using MediatR;

namespace Catalog.Application.Stores;
public sealed record RenameStoreCommand(Guid StoreId, string Name) : IRequest<Unit>;