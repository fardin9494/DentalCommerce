using System.Text.Json;
using Microsoft.Extensions.Logging;
using Sales.Application.Abstractions;

namespace Sales.Infrastructure.Gateways;

public sealed class CatalogStoreGateway : IStoreLookupGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogStoreGateway> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CatalogStoreGateway(IHttpClientFactory httpClientFactory, ILogger<CatalogStoreGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient("CatalogApi");
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, StoreInfo>> GetStoresByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var list = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        if (list.Count == 0) return new Dictionary<Guid, StoreInfo>();

        var tasks = list.Select(id => LoadStoreAsync(id, ct));
        var results = await Task.WhenAll(tasks);

        var dict = new Dictionary<Guid, StoreInfo>();
        foreach (var item in results)
        {
            if (item is null) continue;
            dict[item.Id] = item;
        }

        return dict;
    }

    private async Task<StoreInfo?> LoadStoreAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/stores/{id}", ct);
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode != 404)
                {
                    _logger.LogWarning("Catalog store lookup failed for {StoreId}. Status: {Status}", id, (int)response.StatusCode);
                }
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<StoreDto>(json, JsonOptions);
            if (dto is null || dto.Id == Guid.Empty || string.IsNullOrWhiteSpace(dto.Name))
                return null;

            return new StoreInfo(dto.Id, dto.Name, dto.Domain);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog store lookup failed for {StoreId}", id);
            return null;
        }
    }

    private sealed class StoreDto
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Domain { get; init; }
    }
}
