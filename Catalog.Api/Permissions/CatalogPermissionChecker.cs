using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Permissions;

public interface ICatalogPermissionChecker
{
    Task<bool> HasPermissionAsync(HttpContext ctx, string permissionKey, CancellationToken ct);
}

public sealed class CatalogPermissionChecker : ICatalogPermissionChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CatalogPermissionChecker(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _config = config;
    }

    public async Task<bool> HasPermissionAsync(HttpContext ctx, string permissionKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(permissionKey)) return true;

        if (ctx.User?.Identity?.IsAuthenticated != true)
            return false;

        var sub = ctx.User.FindFirst("sub")?.Value
                  ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId))
            return false;

        var cacheKey = $"cat-perms:{userId}";
        if (!_cache.TryGetValue(cacheKey, out CatalogPermissionDto? cached))
        {
            var identityApiUrl = _config["IdentityApiUrl"];
            if (string.IsNullOrWhiteSpace(identityApiUrl))
                return false;

            var client = _httpClientFactory.CreateClient("IdentityApi");
            if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader.ToString());

            var resp = await client.GetAsync("/api/identity/me/permissions/catalog", ct);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync(ct);
            cached = JsonSerializer.Deserialize<CatalogPermissionDto>(json, JsonOptions);
            _cache.Set(cacheKey, cached, TimeSpan.FromSeconds(30));
        }

        if (cached is null) return false;
        if (cached.IsSuperAdmin) return true;
        return cached.Items.Any(x => x.Key == permissionKey && x.Granted);
    }

    private sealed record CatalogPermissionDto(bool IsSuperAdmin, IReadOnlyList<CatalogPermissionItemDto> Items);
    private sealed record CatalogPermissionItemDto(string Key, bool Granted);
}
