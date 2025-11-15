using MediatR;

namespace Catalog.Application.Brands;

public sealed record UpdateCountryCommand(
    string Code2,
    string NameFa,
    string NameEn,
    string? Region,
    string? FlagEmoji
) : IRequest;

