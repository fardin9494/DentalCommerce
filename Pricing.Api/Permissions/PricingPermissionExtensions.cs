namespace Pricing.Api.Permissions;

public static class PricingPermissionExtensions
{
    public static RouteHandlerBuilder RequirePricingPermission(this RouteHandlerBuilder builder, string permissionKey)
    {
        return builder.AddEndpointFilter(new PricingPermissionFilter(permissionKey));
    }

    public static RouteGroupBuilder RequirePricingPermission(this RouteGroupBuilder builder, string permissionKey)
    {
        builder.AddEndpointFilter(new PricingPermissionFilter(permissionKey));
        return builder;
    }

    public static RouteHandlerBuilder RequirePricingAnyPermission(this RouteHandlerBuilder builder, params string[] permissionKeys)
    {
        return builder.AddEndpointFilter(new PricingAnyPermissionFilter(permissionKeys));
    }

    public static RouteGroupBuilder RequirePricingAnyPermission(this RouteGroupBuilder builder, params string[] permissionKeys)
    {
        builder.AddEndpointFilter(new PricingAnyPermissionFilter(permissionKeys));
        return builder;
    }
}

internal sealed class PricingPermissionFilter : IEndpointFilter
{
    private readonly string _permissionKey;

    public PricingPermissionFilter(string permissionKey)
    {
        _permissionKey = permissionKey;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var checker = ctx.HttpContext.RequestServices.GetRequiredService<IPricingPermissionChecker>();
        var ok = await checker.HasPermissionAsync(ctx.HttpContext, _permissionKey, ctx.HttpContext.RequestAborted);
        if (!ok) return Results.Forbid();
        return await next(ctx);
    }
}

internal sealed class PricingAnyPermissionFilter : IEndpointFilter
{
    private readonly string[] _permissionKeys;

    public PricingAnyPermissionFilter(string[] permissionKeys)
    {
        _permissionKeys = permissionKeys ?? Array.Empty<string>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (_permissionKeys.Length == 0) return await next(ctx);

        var checker = ctx.HttpContext.RequestServices.GetRequiredService<IPricingPermissionChecker>();
        foreach (var key in _permissionKeys)
        {
            if (await checker.HasPermissionAsync(ctx.HttpContext, key, ctx.HttpContext.RequestAborted))
                return await next(ctx);
        }

        return Results.Forbid();
    }
}
