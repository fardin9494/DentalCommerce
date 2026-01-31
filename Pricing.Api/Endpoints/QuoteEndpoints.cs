using MediatR;
using Pricing.Application.Features.Quotes.Commands;
using Pricing.Application.Features.Quotes.Models;

namespace Pricing.Api.Endpoints;

public static class QuoteEndpoints
{
    public static RouteGroupBuilder MapQuoteEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/quote", async (QuoteRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateQuoteCommand(request));
            return Results.Ok(result);
        });

        return group;
    }
}
