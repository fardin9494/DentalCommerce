using FluentValidation;

namespace Pricing.Application.Features.Bulk.Commands;

public sealed class BulkCreateCategoryCampaignValidator : AbstractValidator<BulkCreateCategoryCampaignCommand>
{
    public BulkCreateCategoryCampaignValidator()
    {
        RuleFor(x => x.CategoryIds).NotEmpty();
        RuleFor(x => x.NamePrefix).NotEmpty();
    }
}
