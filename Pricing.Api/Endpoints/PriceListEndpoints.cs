using MediatR;
using Pricing.Application.Features.PriceLists;

namespace Pricing.Api.Endpoints;

public static class PriceListEndpoints
{
    public static RouteGroupBuilder MapPriceListEndpoints(this RouteGroupBuilder group)
    {
        var lists = group.MapGroup("/pricelists");

        lists.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListPriceListsQuery());
            return Results.Ok(result);
        });

        lists.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPriceListByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        lists.MapPost("/", async (CreatePriceListCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/pricing/pricelists/{id}", new { id });
        });

        lists.MapPut("/{id:guid}", async (Guid id, UpdatePriceListBody body, IMediator mediator) =>
        {
            await mediator.Send(new UpdatePriceListCommand(id, body.Name, body.Currency, body.ValidFrom, body.ValidTo, body.IsActive));
            return Results.NoContent();
        });

        lists.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeletePriceListCommand(id));
            return Results.NoContent();
        });

        return group;
    }

    public sealed record UpdatePriceListBody(
        string Name,
        string Currency,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        bool IsActive);
}
