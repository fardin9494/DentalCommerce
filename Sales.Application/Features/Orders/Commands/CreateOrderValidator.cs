using FluentValidation;

namespace Sales.Application.Features.Orders.Commands;

public sealed class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Request.SiteId).NotEmpty();
        RuleFor(x => x.Request.Items).NotNull().NotEmpty();

        RuleForEach(x => x.Request.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.SkuId).NotEmpty();
            item.RuleFor(x => x.Qty).GreaterThan(0);
        });
    }
}

