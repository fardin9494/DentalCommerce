namespace Catalog.Api.Permissions;

public static class CatalogPermissionExtensions
{
    public static RouteHandlerBuilder RequireCatalogPermission(this RouteHandlerBuilder builder, string permissionKey)
    {
        return builder.AddEndpointFilter(new CatalogPermissionFilter(permissionKey));
    }

    public static RouteGroupBuilder RequireCatalogPermission(this RouteGroupBuilder builder, string permissionKey)
    {
        builder.AddEndpointFilter(new CatalogPermissionFilter(permissionKey));
        return builder;
    }

    public static RouteHandlerBuilder RequireCatalogAnyPermission(this RouteHandlerBuilder builder, params string[] permissionKeys)
    {
        return builder.AddEndpointFilter(new CatalogAnyPermissionFilter(permissionKeys));
    }

    public static RouteGroupBuilder RequireCatalogAnyPermission(this RouteGroupBuilder builder, params string[] permissionKeys)
    {
        builder.AddEndpointFilter(new CatalogAnyPermissionFilter(permissionKeys));
        return builder;
    }
}

internal sealed class CatalogPermissionFilter : IEndpointFilter
{
    private readonly string _permissionKey;

    public CatalogPermissionFilter(string permissionKey)
    {
        _permissionKey = permissionKey;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var checker = ctx.HttpContext.RequestServices.GetRequiredService<ICatalogPermissionChecker>();
        var ok = await checker.HasPermissionAsync(ctx.HttpContext, _permissionKey, ctx.HttpContext.RequestAborted);
        if (!ok) return Results.Forbid();
        return await next(ctx);
    }
}

internal sealed class CatalogAnyPermissionFilter : IEndpointFilter
{
    private readonly string[] _permissionKeys;

    public CatalogAnyPermissionFilter(string[] permissionKeys)
    {
        _permissionKeys = permissionKeys ?? Array.Empty<string>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (_permissionKeys.Length == 0) return await next(ctx);

        var checker = ctx.HttpContext.RequestServices.GetRequiredService<ICatalogPermissionChecker>();
        foreach (var key in _permissionKeys)
        {
            if (await checker.HasPermissionAsync(ctx.HttpContext, key, ctx.HttpContext.RequestAborted))
                return await next(ctx);
        }

        return Results.Forbid();
    }
}
