using MediatR;
using Sales.Application.Features.Orders.Commands;
using Sales.Application.Features.Orders.Queries;
using AddOrderNoteRequest = Sales.Application.Features.Orders.Commands.AddOrderNoteRequest;

namespace Sales.Api.Endpoints;

public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/checkout", async (CheckoutOrderRequest request, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new CheckoutOrderCommand(request));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateOrderCommand(request));
            return Results.Ok(result);
        });

        group.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/orders/{id:guid}/timeline", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderTimelineQuery(id));
            return Results.Ok(result);
        });

        group.MapPost("/orders/{id:guid}/ship", async (Guid id, ShipOrderRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new ShipOrderCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/deliver", async (Guid id, DeliverOrderRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new DeliverOrderCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/return", async (Guid id, ReturnOrderRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new ReturnOrderCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/refund", async (Guid id, RefundOrderRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new RefundOrderCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/retry-payment", async (Guid id, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new RetryPaymentCommand(id));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/cancel", async (Guid id, CancelOrderRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new CancelOrderCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/cancel-lines", async (Guid id, CancelOrderLinesRequest body, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new CancelOrderLinesCommand(id, body));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/orders/{id:guid}/refunds", async (Guid id, RequestRefundRequest body, IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new RequestRefundCommand(id, body));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapGet("/orders", async (
            Guid? siteId,
            Guid? userId,
            string? status,
            string? search,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? page,
            int? pageSize,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new ListOrdersQuery(
                SiteId: siteId,
                UserId: userId,
                Status: status,
                Search: search,
                FromUtc: fromUtc,
                ToUtc: toUtc,
                Page: page ?? 1,
                PageSize: pageSize ?? 20));
            return Results.Ok(result);
        });

        group.MapGet("/orders/{id:guid}/notes", async (
            Guid id,
            bool? includeInternal,
            IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderNotesQuery(id, includeInternal ?? true));
            return Results.Ok(result);
        });

        group.MapGet("/orders/{id:guid}/refunds", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetOrderRefundsQuery(id));
            return Results.Ok(result);
        });

        group.MapPost("/orders/{id:guid}/notes", async (
            Guid id,
            AddOrderNoteRequest body,
            IMediator mediator) =>
        {
            try
            {
                var result = await mediator.Send(new AddOrderNoteCommand(id, body));
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/refunds/{id:guid}/approve", async (Guid id, ApproveRefundRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new ApproveRefundCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/refunds/{id:guid}/reject", async (Guid id, RejectRefundRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new RejectRefundCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        group.MapPost("/refunds/{id:guid}/complete", async (Guid id, CompleteRefundRequest body, IMediator mediator) =>
        {
            try
            {
                await mediator.Send(new CompleteRefundCommand(id, body));
                return Results.NoContent();
            }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { message = ex.Message }); }
        });

        return group;
    }
}
