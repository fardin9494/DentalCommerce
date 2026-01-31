using FluentValidation;

namespace Sales.Application.Features.Orders.Commands;

public sealed class RetryPaymentValidator : AbstractValidator<RetryPaymentCommand>
{
    public RetryPaymentValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("OrderId is required.");
    }
}
