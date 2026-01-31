using MediatR;
using Pricing.Application.Features.Overrides;
using Pricing.Domain.Overrides;

namespace Pricing.Api.Endpoints;

public static class OverrideEndpoints
{
    public static RouteGroupBuilder MapOverrideEndpoints(this RouteGroupBuilder group)
    {
        var overrides = group.MapGroup("/overrides");

        overrides.MapGet("/", async (OverrideScope? scopeType, Guid? scopeId, string? skuId, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListPriceOverridesQuery(scopeType, scopeId, skuId));
            return Results.Ok(result);
        });

        overrides.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPriceOverrideByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        overrides.MapPost("/", async (CreatePriceOverrideCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/pricing/overrides/{id}", new { id });
        });

        overrides.MapPut("/{id:guid}", async (Guid id, UpdatePriceOverrideBody body, IMediator mediator) =>
        {
            await mediator.Send(new UpdatePriceOverrideCommand(
                id,
                body.OverrideType,
                body.Value,
                body.Currency,
                body.ValidFrom,
                body.ValidTo,
                body.Priority,
                body.StackingGroup));
            return Results.NoContent();
        });

        overrides.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeletePriceOverrideCommand(id));
            return Results.NoContent();
        });

        return group;
    }

    public sealed record UpdatePriceOverrideBody(
        OverrideType OverrideType,
        decimal Value,
        string Currency,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        int Priority,
        string? StackingGroup);
}
