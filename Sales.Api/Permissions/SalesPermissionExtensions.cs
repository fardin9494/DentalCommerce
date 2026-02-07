namespace Sales.Api.Permissions;

public static class SalesPermissionExtensions
{
    public static RouteHandlerBuilder RequireSalesPermission(this RouteHandlerBuilder builder, string permissionKey)
    {
        return builder.AddEndpointFilter(new SalesPermissionFilter(permissionKey));
    }

    public static RouteGroupBuilder RequireSalesPermission(this RouteGroupBuilder builder, string permissionKey)
    {
        builder.AddEndpointFilter(new SalesPermissionFilter(permissionKey));
        return builder;
    }

    public static RouteHandlerBuilder RequireSalesAnyPermission(this RouteHandlerBuilder builder, params string[] permissionKeys)
    {
        return builder.AddEndpointFilter(new SalesAnyPermissionFilter(permissionKeys));
    }

    public static RouteGroupBuilder RequireSalesAnyPermission(this RouteGroupBuilder builder, params string[] permissionKeys)
    {
        builder.AddEndpointFilter(new SalesAnyPermissionFilter(permissionKeys));
        return builder;
    }
}

internal sealed class SalesPermissionFilter : IEndpointFilter
{
    private readonly string _permissionKey;

    public SalesPermissionFilter(string permissionKey)
    {
        _permissionKey = permissionKey;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var checker = ctx.HttpContext.RequestServices.GetRequiredService<ISalesPermissionChecker>();
        var ok = await checker.HasPermissionAsync(ctx.HttpContext, _permissionKey, ctx.HttpContext.RequestAborted);
        if (!ok) return Results.Forbid();
        return await next(ctx);
    }
}

internal sealed class SalesAnyPermissionFilter : IEndpointFilter
{
    private readonly string[] _permissionKeys;

    public SalesAnyPermissionFilter(string[] permissionKeys)
    {
        _permissionKeys = permissionKeys ?? Array.Empty<string>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (_permissionKeys.Length == 0) return await next(ctx);

        var checker = ctx.HttpContext.RequestServices.GetRequiredService<ISalesPermissionChecker>();
        foreach (var key in _permissionKeys)
        {
            if (await checker.HasPermissionAsync(ctx.HttpContext, key, ctx.HttpContext.RequestAborted))
                return await next(ctx);
        }

        return Results.Forbid();
    }
}
