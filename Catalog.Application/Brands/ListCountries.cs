using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public sealed record ListCountriesQuery(string? Search = null) : IRequest<IReadOnlyList<CountryDto>>;
public sealed class CountryDto
{
    public string Code2 { get; init; } = default!;
    public string Code3 { get; init; } = default!;
    public string NameFa { get; init; } = default!;
    public string NameEn { get; init; } = default!;
    public string? Region { get; init; }
    public string? FlagEmoji { get; init; }
}

public sealed class ListCountriesHandler : IRequestHandler<ListCountriesQuery, IReadOnlyList<CountryDto>>
{
    private readonly DbContext _db;
    public ListCountriesHandler(DbContext db) => _db = db;

    public async Task<IReadOnlyList<CountryDto>> Handle(ListCountriesQuery q, CancellationToken ct)
    {
        var src = _db.Set<Country>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLower();
            src = src.Where(c => c.NameFa.ToLower().Contains(s) || c.NameEn.ToLower().Contains(s) || c.Code2.ToLower() == s || c.Code3.ToLower() == s);
        }

        return await src
            .OrderBy(c => c.NameEn)
            .Select(c => new CountryDto
            {
                Code2 = c.Code2,
                Code3 = c.Code3,
                NameFa = c.NameFa,
                NameEn = c.NameEn,
                Region = c.Region,
                FlagEmoji = c.FlagEmoji
            })
            .ToListAsync(ct);
    }
}