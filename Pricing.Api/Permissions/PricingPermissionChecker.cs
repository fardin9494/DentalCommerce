using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Pricing.Api.Permissions;

public interface IPricingPermissionChecker
{
    Task<bool> HasPermissionAsync(HttpContext ctx, string permissionKey, CancellationToken ct);
}

public sealed class PricingPermissionChecker : IPricingPermissionChecker
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _config;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PricingPermissionChecker(IHttpClientFactory httpClientFactory, IMemoryCache cache, IConfiguration config)
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

        var cacheKey = $"prc-perms:{userId}";
        if (!_cache.TryGetValue(cacheKey, out PricingPermissionDto? cached))
        {
            var identityApiUrl = _config["IdentityApiUrl"];
            if (string.IsNullOrWhiteSpace(identityApiUrl))
                return false;

            var client = _httpClientFactory.CreateClient("IdentityApi");
            if (ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
                client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authHeader.ToString());

            var resp = await client.GetAsync("/api/identity/me/permissions/pricing", ct);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync(ct);
            cached = JsonSerializer.Deserialize<PricingPermissionDto>(json, JsonOptions);
            _cache.Set(cacheKey, cached, TimeSpan.FromSeconds(30));
        }

        if (cached is null) return false;
        if (cached.IsSuperAdmin) return true;
        return cached.Items.Any(x => x.Key == permissionKey && x.Granted);
    }

    private sealed record PricingPermissionDto(bool IsSuperAdmin, IReadOnlyList<PricingPermissionItemDto> Items);
    private sealed record PricingPermissionItemDto(string Key, bool Granted);
}
