using MediatR;

namespace Catalog.Application.Brands;

public sealed record CreateCountryCommand(
    string Code2, string Code3, string NameFa, string NameEn, string? Region, string? FlagEmoji
) : IRequest<string>; // Code2 به عنوان کلید
