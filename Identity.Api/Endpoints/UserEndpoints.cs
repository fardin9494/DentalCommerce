using Identity.Api.Services;
using Identity.Application.Features.Admin.Permissions;
using Identity.Application.Features.Users.Commands;
using Identity.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
    {
        var me = group.MapGroup("/me").RequireAuthorization();

        me.MapGet("/", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetMeQuery(userId));
            return Results.Ok(dto);
        });

        me.MapPatch("/", async (UpdateMeRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new UpdateMeCommand(userId, body));
            return Results.Ok(dto);
        });

        me.MapGet("/sites", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetMySitesQuery(userId));
            return Results.Ok(dto);
        });

        me.MapPost("/password/set", async (SetPasswordRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            await mediator.Send(new SetPasswordCommand(userId, body));
            return Results.NoContent();
        });

        me.MapGet("/permissions/inventory", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetInventoryPermissionsQuery(userId));
            return Results.Ok(dto);
        });

        me.MapGet("/permissions/catalog", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetCatalogPermissionsQuery(userId));
            return Results.Ok(dto);
        });

        me.MapGet("/permissions/pricing", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetPricingPermissionsQuery(userId));
            return Results.Ok(dto);
        });

        me.MapGet("/permissions/sales", async (HttpContext ctx, IMediator mediator) =>
        {
            var userId = ClaimsHelper.GetUserId(ctx.User);
            var dto = await mediator.Send(new GetSalesPermissionsQuery(userId));
            return Results.Ok(dto);
        });

        return group;
    }
}
