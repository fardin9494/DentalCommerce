using Pricing.Application.Abstractions;
using Pricing.Application.Features.Quotes.Models;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Services;

public sealed class EligibilityEvaluator
{
    public EligibilityResult Evaluate(
        EligibilityDefinition definition,
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp)
    {
        var itemSkus = request.Items.Select(x => x.SkuId).Distinct().ToList();
        var allSkus = new HashSet<string>(itemSkus, StringComparer.OrdinalIgnoreCase);

        return definition switch
        {
            AllEligibilityDefinition => new EligibilityResult(true, allSkus, null),
            ProductEligibilityDefinition product => EvaluateProduct(product, allSkus),
            CategoryEligibilityDefinition category => EvaluateCategory(category, allSkus, catalog),
            BrandEligibilityDefinition brand => EvaluateBrand(brand, allSkus, catalog),
            TagEligibilityDefinition tag => EvaluateTag(tag, allSkus, catalog),
            SiteEligibilityDefinition site => EvaluateSite(site, request.SiteId, allSkus),
            UserEligibilityDefinition user => EvaluateUser(user, request.UserId, allSkus),
            SeasonalEligibilityDefinition seasonal => EvaluateSeasonal(seasonal, timestamp, allSkus),
            BundleEligibilityDefinition bundle => EvaluateBundle(bundle, request, allSkus),
            BatchExpiryBeforeEligibilityDefinition batch => EvaluateBatchExpiry(batch, request, batches, timestamp),
            AllOfEligibilityDefinition allOf => EvaluateAllOf(allOf, request, catalog, batches, timestamp, allSkus),
            AnyOfEligibilityDefinition anyOf => EvaluateAnyOf(anyOf, request, catalog, batches, timestamp, allSkus),
            _ => new EligibilityResult(false, new HashSet<string>(), "Unknown eligibility type.")
        };
    }

    private static EligibilityResult EvaluateProduct(ProductEligibilityDefinition definition, HashSet<string> allSkus)
    {
        var eligible = new HashSet<string>(definition.SkuIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        eligible.IntersectWith(allSkus);
        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No matching SKU for product eligibility.")
            : new EligibilityResult(true, eligible, null);
    }

    private static EligibilityResult EvaluateCategory(
        CategoryEligibilityDefinition definition,
        HashSet<string> allSkus,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog)
    {
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in allSkus)
        {
            if (catalog.TryGetValue(sku, out var info) && info.CategoryIds.Any(id => definition.CategoryIds.Contains(id)))
                eligible.Add(sku);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No matching category in cart.")
            : new EligibilityResult(true, eligible, null);
    }

    private static EligibilityResult EvaluateBrand(
        BrandEligibilityDefinition definition,
        HashSet<string> allSkus,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog)
    {
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in allSkus)
        {
            if (catalog.TryGetValue(sku, out var info) && info.BrandId.HasValue && definition.BrandIds.Contains(info.BrandId.Value))
                eligible.Add(sku);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No matching brand in cart.")
            : new EligibilityResult(true, eligible, null);
    }

    private static EligibilityResult EvaluateTag(
        TagEligibilityDefinition definition,
        HashSet<string> allSkus,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog)
    {
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sku in allSkus)
        {
            if (catalog.TryGetValue(sku, out var info) && info.Tags.Any(t => definition.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                eligible.Add(sku);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No matching tag in cart.")
            : new EligibilityResult(true, eligible, null);
    }

    private static EligibilityResult EvaluateSite(SiteEligibilityDefinition definition, Guid siteId, HashSet<string> allSkus)
    {
        return definition.SiteIds.Contains(siteId)
            ? new EligibilityResult(true, allSkus, null)
            : new EligibilityResult(false, new HashSet<string>(), "Site not eligible.");
    }

    private static EligibilityResult EvaluateUser(UserEligibilityDefinition definition, Guid? userId, HashSet<string> allSkus)
    {
        if (!userId.HasValue) return new EligibilityResult(false, new HashSet<string>(), "User not provided.");

        return definition.UserIds.Contains(userId.Value)
            ? new EligibilityResult(true, allSkus, null)
            : new EligibilityResult(false, new HashSet<string>(), "User not eligible.");
    }

    private static EligibilityResult EvaluateSeasonal(SeasonalEligibilityDefinition definition, DateTime timestamp, HashSet<string> allSkus)
    {
        var withinStart = !definition.From.HasValue || definition.From.Value <= timestamp;
        var withinEnd = !definition.To.HasValue || definition.To.Value >= timestamp;
        return withinStart && withinEnd
            ? new EligibilityResult(true, allSkus, null)
            : new EligibilityResult(false, new HashSet<string>(), "Outside campaign date range.");
    }

    private static EligibilityResult EvaluateBundle(BundleEligibilityDefinition definition, QuoteRequest request, HashSet<string> allSkus)
    {
        foreach (var requirement in definition.Requirements)
        {
            var line = request.Items.FirstOrDefault(i => string.Equals(i.SkuId, requirement.SkuId, StringComparison.OrdinalIgnoreCase));
            if (line is null || line.Qty < requirement.Qty)
                return new EligibilityResult(false, new HashSet<string>(), "Bundle requirements not met.");
        }

        var eligible = new HashSet<string>(definition.Requirements.Select(r => r.SkuId), StringComparer.OrdinalIgnoreCase);
        eligible.IntersectWith(allSkus);
        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "Bundle requirements not met.")
            : new EligibilityResult(true, eligible, null);
    }

    private static EligibilityResult EvaluateBatchExpiry(
        BatchExpiryBeforeEligibilityDefinition definition,
        QuoteRequest request,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp)
    {
        if (!definition.Date.HasValue && !definition.WithinDays.HasValue)
            return new EligibilityResult(false, new HashSet<string>(), "Batch expiry threshold not configured.");

        var threshold = definition.Date ?? timestamp.AddDays(definition.WithinDays!.Value);
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            if (!item.BatchId.HasValue) continue;
            if (!batches.TryGetValue(item.BatchId.Value, out var batch)) continue;

            if (batch.ExpiryDate.HasValue && batch.ExpiryDate.Value <= threshold)
                eligible.Add(item.SkuId);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No batch matches expiry criteria.")
            : new EligibilityResult(true, eligible, null);
    }

    private EligibilityResult EvaluateAllOf(
        AllOfEligibilityDefinition definition,
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp,
        HashSet<string> allSkus)
    {
        var eligible = new HashSet<string>(allSkus, StringComparer.OrdinalIgnoreCase);
        foreach (var condition in definition.Conditions)
        {
            var result = Evaluate(condition, request, catalog, batches, timestamp);
            if (!result.IsEligible)
                return new EligibilityResult(false, new HashSet<string>(), result.Reason ?? "Eligibility condition failed.");

            eligible.IntersectWith(result.EligibleSkuIds);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "Eligibility conditions produced no matching items.")
            : new EligibilityResult(true, eligible, null);
    }

    private EligibilityResult EvaluateAnyOf(
        AnyOfEligibilityDefinition definition,
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp,
        HashSet<string> allSkus)
    {
        var eligible = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in definition.Conditions)
        {
            var result = Evaluate(condition, request, catalog, batches, timestamp);
            if (result.IsEligible)
                eligible.UnionWith(result.EligibleSkuIds);
        }

        return eligible.Count == 0
            ? new EligibilityResult(false, eligible, "No eligibility condition matched.")
            : new EligibilityResult(true, eligible, null);
    }
}
