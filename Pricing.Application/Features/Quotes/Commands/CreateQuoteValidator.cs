using FluentValidation;

namespace Pricing.Application.Features.Quotes.Commands;

public sealed class CreateQuoteValidator : AbstractValidator<CreateQuoteCommand>
{
    public CreateQuoteValidator()
    {
        RuleFor(x => x.Request.SiteId).NotEmpty();
        RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.SkuId).NotEmpty();
            item.RuleFor(i => i.Qty).GreaterThan(0);
        });
    }
}
