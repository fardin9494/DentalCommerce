using FluentValidation;

namespace Pricing.Application.Features.Campaigns;

public sealed class CreatePromotionCampaignValidator : AbstractValidator<CreatePromotionCampaignCommand>
{
    public CreatePromotionCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.StackingGroup).NotEmpty().WithMessage("StackingGroup is required.");
    }
}

public sealed class UpdatePromotionCampaignValidator : AbstractValidator<UpdatePromotionCampaignCommand>
{
    public UpdatePromotionCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.StackingGroup).NotEmpty().WithMessage("StackingGroup is required.");
    }
}
