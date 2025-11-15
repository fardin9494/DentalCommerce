using FluentValidation;

namespace Catalog.Application.Brands;

public sealed class UpdateCountryValidator : AbstractValidator<UpdateCountryCommand>
{
    public UpdateCountryValidator()
    {
        RuleFor(x => x.Code2).NotEmpty().Length(2);
        RuleFor(x => x.NameFa).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Region).MaximumLength(64).When(x => x.Region != null);
        RuleFor(x => x.FlagEmoji).MaximumLength(8).When(x => x.FlagEmoji != null);
    }
}

