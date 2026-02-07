using MediatR;
using Pricing.Api.Permissions;
using Pricing.Application.Features.Campaigns;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Api.Endpoints;

public static class CampaignEndpoints
{
    public static RouteGroupBuilder MapCampaignEndpoints(this RouteGroupBuilder group)
    {
        var campaigns = group.MapGroup("/campaigns");

        campaigns.MapGet("/", async (bool? isActive, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListPromotionCampaignsQuery(isActive));
            return Results.Ok(result);
        }).RequirePricingPermission(PricingPermissionKeys.CampaignsView);

        campaigns.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPromotionCampaignByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).RequirePricingPermission(PricingPermissionKeys.CampaignsDetailView);

        campaigns.MapPost("/", async (CreatePromotionCampaignCommand cmd, IMediator mediator) =>
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/pricing/campaigns/{id}", new { id });
        }).RequirePricingPermission(PricingPermissionKeys.CampaignsCreate);

        campaigns.MapPut("/{id:guid}", async (Guid id, UpdatePromotionCampaignBody body, IMediator mediator) =>
        {
            await mediator.Send(new UpdatePromotionCampaignCommand(
                id,
                body.Name,
                body.IsActive,
                body.ValidFrom,
                body.ValidTo,
                body.Priority,
                body.StackingGroup,
                body.StackingMode,
                body.CombinableWithOtherPromotions,
                body.CombinableWithCoupons,
                body.ExclusiveGroup,
                body.Guardrails,
                body.Eligibility,
                body.Benefit));
            return Results.NoContent();
        }).RequirePricingPermission(PricingPermissionKeys.CampaignsEdit);

        campaigns.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            await mediator.Send(new DeletePromotionCampaignCommand(id));
            return Results.NoContent();
        }).RequirePricingPermission(PricingPermissionKeys.CampaignsDelete);

        return group;
    }

    public sealed record UpdatePromotionCampaignBody(
        string Name,
        bool IsActive,
        DateTime? ValidFrom,
        DateTime? ValidTo,
        int Priority,
        string? StackingGroup,
        StackingMode StackingMode,
        bool CombinableWithOtherPromotions,
        bool CombinableWithCoupons,
        string? ExclusiveGroup,
        Guardrails? Guardrails,
        EligibilityDefinition Eligibility,
        BenefitDefinition Benefit);
}
