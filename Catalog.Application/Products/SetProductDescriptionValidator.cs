using FluentValidation;

namespace Catalog.Application.Products;

public class SetProductDescriptionValidator : AbstractValidator<SetProductDescriptionCommand>
{
	public SetProductDescriptionValidator()
	{
		// اگر خواستی حد بگذاریم (مثلاً 200 هزار کاراکتر)
		RuleFor(x => x.ContentHtml)
			.Must(x => x == null || x.Length <= 200_000)
			.WithMessage("طول توضیحات نباید بیش از 200,000 کاراکتر باشد.");
	}
}