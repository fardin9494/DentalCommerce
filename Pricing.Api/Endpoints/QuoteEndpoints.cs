using MediatR;
using Pricing.Api.Permissions;
using Pricing.Application.Features.Quotes.Commands;
using Pricing.Application.Features.Quotes.Models;

namespace Pricing.Api.Endpoints;

public static class QuoteEndpoints
{
    public static RouteGroupBuilder MapQuoteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/quote", async (QuoteRequest request, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new CreateQuoteCommand(request));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        }).RequirePricingPermission(PricingPermissionKeys.QuotesCreate);

        return group;
    }
}
