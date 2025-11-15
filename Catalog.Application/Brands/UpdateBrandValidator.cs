using FluentValidation;

namespace Catalog.Application.Brands;

public sealed class UpdateBrandValidator : AbstractValidator<UpdateBrandCommand>
{
    public UpdateBrandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Website)
            .MaximumLength(512)
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.Description)
            .MaximumLength(4000)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.EstablishedYear)
            .InclusiveBetween(1800, DateTime.UtcNow.Year)
            .When(x => x.EstablishedYear.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
