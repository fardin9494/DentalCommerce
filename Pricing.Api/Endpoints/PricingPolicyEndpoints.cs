using MediatR;
using Pricing.Application.Features.Policies;
using Pricing.Domain.Common;

namespace Pricing.Api.Endpoints;

public static class PricingPolicyEndpoints
{
    public static RouteGroupBuilder MapPricingPolicyEndpoints(this RouteGroupBuilder group)
    {
        var policies = group.MapGroup("/policies");

        policies.MapGet("/list", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPricingPoliciesQuery());
            return Results.Ok(result);
        });

        policies.MapGet("/", async (Guid? siteId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPricingPolicyQuery(siteId));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        policies.MapPut("/", async (UpsertPricingPolicyBody body, IMediator mediator) =>
        {
            var id = await mediator.Send(new UpsertPricingPolicyCommand(body.SiteId, body.DefaultStackingMode));
            return Results.Ok(new { id });
        });

        return group;
    }

    public sealed record UpsertPricingPolicyBody(Guid? SiteId, StackingMode DefaultStackingMode);
}
