using Pricing.Domain.Coupons;
using Pricing.Domain.Overrides;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Policies;
using Pricing.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Pricing.Application.Abstractions;

public interface IPricingDbContext
{
    DbSet<PriceList> PriceLists { get; }
    DbSet<PriceListItem> PriceListItems { get; }
    DbSet<PriceOverride> PriceOverrides { get; }
    DbSet<PromotionCampaign> PromotionCampaigns { get; }
    DbSet<Coupon> Coupons { get; }
    DbSet<CouponRedemption> CouponRedemptions { get; }
    DbSet<PricingPolicy> PricingPolicies { get; }

    ChangeTracker ChangeTracker { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
