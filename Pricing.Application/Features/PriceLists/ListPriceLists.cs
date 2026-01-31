using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.PriceLists;

namespace Pricing.Application.Features.PriceLists;

public sealed record ListPriceListsQuery() : IRequest<IReadOnlyList<PriceListDto>>;

public sealed class ListPriceListsHandler : IRequestHandler<ListPriceListsQuery, IReadOnlyList<PriceListDto>>
{
    private readonly Abstractions.IPricingDbContext _db;

    public ListPriceListsHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<PriceListDto>> Handle(ListPriceListsQuery request, CancellationToken ct)
    {
        var lists = await _db.PriceLists
            .Include(x => x.Items)
            .AsNoTracking()
            .ToListAsync(ct);

        return lists.Select(PriceListMapper.Map).ToList();
    }
}

public sealed record GetPriceListByIdQuery(Guid Id) : IRequest<PriceListDto?>;

public sealed class GetPriceListByIdHandler : IRequestHandler<GetPriceListByIdQuery, PriceListDto?>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetPriceListByIdHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<PriceListDto?> Handle(GetPriceListByIdQuery request, CancellationToken ct)
    {
        var entity = await _db.PriceLists
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        return entity is null ? null : PriceListMapper.Map(entity);
    }
}
