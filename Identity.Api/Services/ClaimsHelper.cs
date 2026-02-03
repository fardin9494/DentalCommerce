using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Identity.Api.Services;

public static class ClaimsHelper
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static Guid GetSessionId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("sid")
                  ?? user.FindFirstValue(ClaimTypes.Sid);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static Guid GetSiteId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("siteId");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}

