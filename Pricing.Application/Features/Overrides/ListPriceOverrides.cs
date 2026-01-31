using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Overrides;

namespace Pricing.Application.Features.Overrides;

public sealed record ListPriceOverridesQuery(OverrideScope? ScopeType = null, Guid? ScopeId = null, string? SkuId = null)
    : IRequest<IReadOnlyList<PriceOverrideDto>>;

public sealed class ListPriceOverridesHandler : IRequestHandler<ListPriceOverridesQuery, IReadOnlyList<PriceOverrideDto>>
{
    private readonly Abstractions.IPricingDbContext _db;

    public ListPriceOverridesHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<PriceOverrideDto>> Handle(ListPriceOverridesQuery request, CancellationToken ct)
    {
        var query = _db.PriceOverrides.AsNoTracking().AsQueryable();
        if (request.ScopeType.HasValue)
            query = query.Where(x => x.ScopeType == request.ScopeType.Value);
        if (request.ScopeId.HasValue)
            query = query.Where(x => x.ScopeId == request.ScopeId.Value);
        if (!string.IsNullOrWhiteSpace(request.SkuId))
            query = query.Where(x => x.SkuId == request.SkuId);

        var items = await query.ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    internal static PriceOverrideDto Map(PriceOverride entity)
    {
        return new PriceOverrideDto
        {
            Id = entity.Id,
            ScopeType = entity.ScopeType,
            ScopeId = entity.ScopeId,
            SkuId = entity.SkuId,
            OverrideType = entity.OverrideType,
            Value = entity.Value,
            Currency = entity.Currency,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            Priority = entity.Priority,
            StackingGroup = entity.StackingGroup
        };
    }
}

public sealed record GetPriceOverrideByIdQuery(Guid Id) : IRequest<PriceOverrideDto?>;

public sealed class GetPriceOverrideByIdHandler : IRequestHandler<GetPriceOverrideByIdQuery, PriceOverrideDto?>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetPriceOverrideByIdHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<PriceOverrideDto?> Handle(GetPriceOverrideByIdQuery request, CancellationToken ct)
    {
        var entity = await _db.PriceOverrides.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? null : ListPriceOverridesHandler.Map(entity);
    }
}
