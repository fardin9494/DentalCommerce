namespace Inventory.Api.Permissions;

public static class InventoryPermissionExtensions
{
    public static RouteHandlerBuilder RequireInventoryPermission(this RouteHandlerBuilder builder, string permissionKey)
    {
        return builder.AddEndpointFilter(new InventoryPermissionFilter(permissionKey));
    }

    public static RouteGroupBuilder RequireInventoryPermission(this RouteGroupBuilder builder, string permissionKey)
    {
        builder.AddEndpointFilter(new InventoryPermissionFilter(permissionKey));
        return builder;
    }

    public static RouteHandlerBuilder RequireInventoryAnyPermission(this RouteHandlerBuilder builder, params string[] permissionKeys)
    {
        return builder.AddEndpointFilter(new InventoryAnyPermissionFilter(permissionKeys));
    }

    public static RouteGroupBuilder RequireInventoryAnyPermission(this RouteGroupBuilder builder, params string[] permissionKeys)
    {
        builder.AddEndpointFilter(new InventoryAnyPermissionFilter(permissionKeys));
        return builder;
    }
}

internal sealed class InventoryPermissionFilter : IEndpointFilter
{
    private readonly string _permissionKey;

    public InventoryPermissionFilter(string permissionKey)
    {
        _permissionKey = permissionKey;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var checker = ctx.HttpContext.RequestServices.GetRequiredService<IInventoryPermissionChecker>();
        var ok = await checker.HasPermissionAsync(ctx.HttpContext, _permissionKey, ctx.HttpContext.RequestAborted);
        if (!ok) return Results.Forbid();
        return await next(ctx);
    }
}

internal sealed class InventoryAnyPermissionFilter : IEndpointFilter
{
    private readonly string[] _permissionKeys;

    public InventoryAnyPermissionFilter(string[] permissionKeys)
    {
        _permissionKeys = permissionKeys ?? Array.Empty<string>();
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        if (_permissionKeys.Length == 0) return await next(ctx);

        var checker = ctx.HttpContext.RequestServices.GetRequiredService<IInventoryPermissionChecker>();
        foreach (var key in _permissionKeys)
        {
            if (await checker.HasPermissionAsync(ctx.HttpContext, key, ctx.HttpContext.RequestAborted))
                return await next(ctx);
        }

        return Results.Forbid();
    }
}
