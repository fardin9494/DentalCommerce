using MediatR;
using Sales.Application.Features.Reports;

namespace Sales.Api.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/reports/daily", async (DateTime? fromUtc, DateTime? toUtc, Guid? siteId, string? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new DailySalesReportQuery(fromUtc, toUtc, siteId, status));
            return Results.Ok(result);
        });

        group.MapGet("/reports/monthly", async (DateTime? fromUtc, DateTime? toUtc, Guid? siteId, string? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new MonthlySalesReportQuery(fromUtc, toUtc, siteId, status));
            return Results.Ok(result);
        });

        group.MapGet("/reports/by-site", async (DateTime? fromUtc, DateTime? toUtc, string? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new SalesBySiteQuery(fromUtc, toUtc, status));
            return Results.Ok(result);
        });

        group.MapGet("/reports/by-product", async (DateTime? fromUtc, DateTime? toUtc, Guid? siteId, string? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new SalesByProductQuery(fromUtc, toUtc, siteId, status));
            return Results.Ok(result);
        });

        group.MapGet("/reports/by-customer", async (DateTime? fromUtc, DateTime? toUtc, Guid? siteId, string? status, IMediator mediator) =>
        {
            var result = await mediator.Send(new SalesByCustomerQuery(fromUtc, toUtc, siteId, status));
            return Results.Ok(result);
        });

        return group;
    }
}
