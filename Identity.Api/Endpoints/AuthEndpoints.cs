using Identity.Api.Services;
using Identity.Application.Features.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Identity.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        var auth = group.MapGroup("/auth");

        auth.MapPost("/otp/request", async (RequestOtpRequest body, IMediator mediator) =>
        {
            var result = await mediator.Send(new RequestOtpCommand(body));
            return Results.Ok(result);
        });

        auth.MapPost("/otp/verify", async (VerifyOtpRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var req = new VerifyOtpRequest
            {
                PhoneNumber = body.PhoneNumber,
                SiteId = body.SiteId,
                Code = body.Code,
                UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString()
            };

            var result = await mediator.Send(new VerifyOtpCommand(req));
            return Results.Ok(result);
        });

        auth.MapPost("/password/login", async (PasswordLoginRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var req = new PasswordLoginRequest
            {
                PhoneNumber = body.PhoneNumber,
                SiteId = body.SiteId,
                Password = body.Password,
                UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString()
            };

            var result = await mediator.Send(new PasswordLoginCommand(req));
            return Results.Ok(result);
        });

        auth.MapPost("/refresh", async (RefreshTokensRequest body, HttpContext ctx, IMediator mediator) =>
        {
            var req = new RefreshTokensRequest
            {
                RefreshToken = body.RefreshToken,
                UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString()
            };

            var result = await mediator.Send(new RefreshTokensCommand(req));
            return Results.Ok(result);
        });

        auth.MapPost("/logout", [Authorize] async (HttpContext ctx, IMediator mediator) =>
        {
            var sessionId = ClaimsHelper.GetSessionId(ctx.User);
            await mediator.Send(new LogoutCommand(sessionId));
            return Results.NoContent();
        });

        return group;
    }
}

