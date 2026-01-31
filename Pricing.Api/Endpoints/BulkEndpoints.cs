using MediatR;
using Pricing.Application.Features.Bulk.Commands;

namespace Pricing.Api.Endpoints;

public static class BulkEndpoints
{
    public static RouteGroupBuilder MapBulkEndpoints(this RouteGroupBuilder group)
    {
        var bulk = group.MapGroup("/bulk");

        bulk.MapPost("/prices", async (BulkUpdatePricesCommand cmd, IMediator mediator) =>
        {
            var updated = await mediator.Send(cmd);
            return Results.Ok(new { updated });
        });

        bulk.MapPost("/campaigns/category", async (BulkCreateCategoryCampaignCommand cmd, IMediator mediator) =>
        {
            var created = await mediator.Send(cmd);
            return Results.Ok(new { created });
        });

        return group;
    }
}
