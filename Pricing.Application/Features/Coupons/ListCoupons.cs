using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Coupons;

namespace Pricing.Application.Features.Coupons;

public sealed record ListCouponsQuery(bool? IsActive = null, string? Code = null) : IRequest<IReadOnlyList<CouponDto>>;

public sealed class ListCouponsHandler : IRequestHandler<ListCouponsQuery, IReadOnlyList<CouponDto>>
{
    private readonly Abstractions.IPricingDbContext _db;

    public ListCouponsHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<IReadOnlyList<CouponDto>> Handle(ListCouponsQuery request, CancellationToken ct)
    {
        var query = _db.Coupons.AsNoTracking().AsQueryable();
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Code))
            query = query.Where(x => x.Code == request.Code.ToUpperInvariant());

        var items = await query.ToListAsync(ct);
        return items.Select(Map).ToList();
    }

    internal static CouponDto Map(Coupon entity)
    {
        return new CouponDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            IsActive = entity.IsActive,
            ValidFrom = entity.ValidFrom,
            ValidTo = entity.ValidTo,
            MaxUsesTotal = entity.MaxUsesTotal,
            MaxUsesPerUser = entity.MaxUsesPerUser,
            Priority = entity.Priority,
            CombinableWithPromotions = entity.CombinableWithPromotions,
            ExclusiveGroup = entity.ExclusiveGroup,
            Guardrails = entity.Guardrails,
            Eligibility = entity.Eligibility,
            Benefit = entity.Benefit
        };
    }
}

public sealed record GetCouponByIdQuery(Guid Id) : IRequest<CouponDto?>;

public sealed class GetCouponByIdHandler : IRequestHandler<GetCouponByIdQuery, CouponDto?>
{
    private readonly Abstractions.IPricingDbContext _db;

    public GetCouponByIdHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<CouponDto?> Handle(GetCouponByIdQuery request, CancellationToken ct)
    {
        var entity = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return entity is null ? null : ListCouponsHandler.Map(entity);
    }
}
