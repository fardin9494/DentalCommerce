using Pricing.Application.Abstractions;
using BuildingBlocks.Domain;
using Pricing.Domain.Coupons;
using Pricing.Domain.Overrides;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Policies;
using Pricing.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Pricing.Infrastructure.Persistence;

public sealed class PricingDbContext : DbContext, IPricingDbContext
{
    public const string DefaultSchema = "pricing";

    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options) { }

    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<PriceOverride> PriceOverrides => Set<PriceOverride>();
    public DbSet<PromotionCampaign> PromotionCampaigns => Set<PromotionCampaign>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponRedemption> CouponRedemptions => Set<CouponRedemption>();
    public DbSet<PricingPolicy> PricingPolicies => Set<PricingPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        IgnoreRowVersion(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void IgnoreRowVersion(ModelBuilder modelBuilder)
    {
        const string rowVersionPropertyName = nameof(AggregateRoot<Guid>.RowVersion);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var prop = entityType.FindProperty(rowVersionPropertyName) ?? entityType.FindProperty("RowVersion");
            if (prop is null) continue;
            modelBuilder.Entity(entityType.ClrType).Ignore(prop.Name);
        }
    }
}
