using MediatR;
using Pricing.Application.Features.Coupons;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Api.Endpoints;

public static class CouponEndpoints
{
    public static RouteGroupBuilder MapCouponEndpoints(this RouteGroupBuilder group)
    {
        var coupons = group.MapGroup("/coupons");

        coupons.MapGet("/", async (bool? isActive, string? code, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCouponsQuery(isActive, code));
            return Results.Ok(result);
        });

        coupons.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCouponByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        coupons.MapGet("/{id:guid}/usage", async (Guid id, Guid? userId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCouponUsageQuery(id, userId));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        coupons.MapGet("/{id:guid}/reservations", async (Guid id, bool? activeOnly, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCouponReservationsQuery(id, activeOnly ?? true));
            return Results.Ok(result);
        });

        coupons.MapPost("/", async (CreateCouponCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/pricing/coupons/{id}", new { id });
        });

        coupons.MapPost("/{code}/reserve", async (string code, ReserveCouponBody body, IMediator mediator) =>
        {
            var result = await mediator.Send(new ReserveCouponCommand(code, body.SiteId, body.UserId, body.CartHash, body.DurationMinutes));
            return Results.Ok(result);
        });

        coupons.MapPut("/{id:guid}", async (Guid id, UpdateCouponBody body, IMediator mediator) =>
        {
            await mediator.Send(new UpdateCouponCommand(
                id,
                body.Code,
                body.Name,
                body.IsActive,
                body.ValidFrom,
                body.ValidTo,
                body.MaxUsesTotal,
                body.MaxUsesPerUser,
                body.Priority,
                body.CombinableWithPromotions,
                body.ExclusiveGroup,
                body.Guardrails,
                body.Eligibility,
                body.Benefit));
            return Results.NoContent();
        });

        coupons.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeleteCouponCommand(id));
            return Results.NoContent();
        });

        return group;
    }

    public sealed record UpdateCouponBody(
        string Code,
        string? Name,
        bool IsActive,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        int? MaxUsesTotal,
        int? MaxUsesPerUser,
        int Priority,
        bool CombinableWithPromotions,
        string? ExclusiveGroup,
        Guardrails? Guardrails,
        EligibilityDefinition Eligibility,
        BenefitDefinition Benefit);

    public sealed record ReserveCouponBody(Guid? SiteId, Guid? UserId, string? CartHash, int? DurationMinutes);
}
