namespace Sales.Application.Abstractions;

public interface IStoreLookupGateway
{
    Task<IReadOnlyDictionary<Guid, StoreInfo>> GetStoresByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

public sealed record StoreInfo(Guid Id, string Name, string? Domain);
