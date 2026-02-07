using Identity.Application.Features.Admin.Permissions;
using Identity.Application.Features.Admin.Reports;
using Identity.Application.Features.Admin.Users.Commands;
using Identity.Application.Features.Admin.Users.Queries;
using Identity.Application.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Api.Endpoints;

public static class AdminEndpoints
{
    public static RouteGroupBuilder MapAdminEndpoints(this RouteGroupBuilder group)
    {
        var admin = group.MapGroup("/admin").RequireAuthorization();

        admin.MapGet("/users", async (HttpRequest req, IMediator mediator) =>
        {
            var query = req.Query["q"].ToString();
            var status = req.Query["status"].ToString();
            var page = int.TryParse(req.Query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? ps : 50;
            var result = await mediator.Send(new GetUsersQuery(query, status, page, pageSize));
            return Results.Ok(result);
        });

        admin.MapGet("/users/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetUserDetailsQuery(id));
            return Results.Ok(result);
        });

        admin.MapGet("/users/{id:guid}/audit", async (Guid id, HttpRequest req, IMediator mediator) =>
        {
            var page = int.TryParse(req.Query["page"], out var p) ? p : 1;
            var pageSize = int.TryParse(req.Query["pageSize"], out var ps) ? ps : 50;
            var result = await mediator.Send(new GetUserAuditQuery(id, page, pageSize));
            return Results.Ok(result);
        });

        admin.MapGet("/users/{id:guid}/permissions/inventory", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetInventoryPermissionsQuery(id));
            return Results.Ok(result);
        });

        admin.MapPost("/users/{id:guid}/permissions/inventory", async (Guid id, UpdateInventoryPermissionsRequest body, IMediator mediator) =>
        {
            await mediator.Send(new UpdateInventoryPermissionsCommand(id, body));
            return Results.NoContent();
        });

        admin.MapGet("/users/{id:guid}/permissions/catalog", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCatalogPermissionsQuery(id));
            return Results.Ok(result);
        });

        admin.MapPost("/users/{id:guid}/permissions/catalog", async (Guid id, UpdateCatalogPermissionsRequest body, IMediator mediator) =>
        {
            await mediator.Send(new UpdateCatalogPermissionsCommand(id, body));
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/ban", async (Guid id, BanUserRequest body, IMediator mediator) =>
        {
            await mediator.Send(new BanUserCommand(id, body));
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/unban", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new UnbanUserCommand(id));
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/unlock", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new UnlockUserCommand(id));
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/sessions/revoke", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new RevokeUserSessionsCommand(id));
            return Results.NoContent();
        });

        admin.MapPost("/users/{id:guid}/password/reset", async (Guid id, ResetUserPasswordRequest body, IMediator mediator) =>
        {
            await mediator.Send(new ResetUserPasswordCommand(id, body));
            return Results.NoContent();
        });

        admin.MapGet("/reports/summary", async (HttpRequest req, IMediator mediator) =>
        {
            var days = int.TryParse(req.Query["days"], out var d) ? d : 30;
            var result = await mediator.Send(new GetIdentityReportQuery(days));
            return Results.Ok(result);
        });

        return group;
    }
}
