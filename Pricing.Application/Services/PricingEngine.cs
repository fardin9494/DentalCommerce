using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Pricing.Application.Abstractions;
using Pricing.Application.Features.Quotes.Models;
using Pricing.Domain.Common;
using Pricing.Domain.Coupons;
using Pricing.Domain.Overrides;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Promotions;
using Pricing.Domain.Promotions.Definitions;
using Pricing.Domain.Quotes;

namespace Pricing.Application.Services;

public sealed class PricingEngine
{
    private const string SourceTypePromotion = "promotion";
    private const string SourceTypeCoupon = "coupon";
    private const string SourceTypeOverride = "override";
    private const int BatchValidationConcurrency = 8;

    private readonly IPricingDbContext _db;
    private readonly ICatalogPricingGateway _catalog;
    private readonly IInventoryBatchInfoGateway _inventory;
    private readonly IClock _clock;
    private readonly EligibilityEvaluator _eligibility;
    private readonly PromotionSelector _selector;
    private readonly BenefitApplier _benefits;
    private readonly GuardrailEnforcer _guardrails;

    public PricingEngine(
        IPricingDbContext db,
        ICatalogPricingGateway catalog,
        IInventoryBatchInfoGateway inventory,
        IClock clock,
        EligibilityEvaluator eligibility,
        PromotionSelector selector,
        BenefitApplier benefits,
        GuardrailEnforcer guardrails)
    {
        _db = db;
        _catalog = catalog;
        _inventory = inventory;
        _clock = clock;
        _eligibility = eligibility;
        _selector = selector;
        _benefits = benefits;
        _guardrails = guardrails;
    }

    public async Task<PriceQuote> CreateQuoteAsync(QuoteRequest request, CancellationToken ct)
    {
        if (request.Items.Count == 0) throw new InvalidOperationException("Quote requires at least one item.");

        var timestamp = request.Timestamp ?? _clock.UtcNow;
        var priceList = await LoadActivePriceListAsync(timestamp, ct);

        var quote = new PriceQuote
        {
            SiteId = request.SiteId,
            UserId = request.UserId,
            Currency = priceList.Currency,
            Timestamp = timestamp
        };

        var lineMap = BuildLines(request, priceList, quote);

        await ApplyOverridesAsync(request, timestamp, priceList, lineMap, quote, ct);

        var skuIds = lineMap.Keys.ToList();
        var catalogInfo = new Dictionary<string, CatalogSkuInfo>(
            await _catalog.GetSkuInfosAsync(skuIds, ct),
            StringComparer.OrdinalIgnoreCase);
        EnsureCatalogResolved(skuIds, catalogInfo, quote);

        var batchInfo = await ValidateBatchesAsync(request, catalogInfo, ct);

        var eligiblePromotions = await LoadEligiblePromotionsAsync(request, catalogInfo, batchInfo, timestamp, quote, ct);

        var policyMode = await LoadStackingModeAsync(request.SiteId, quote, ct);
        var groupOverrides = BuildGroupStackingOverrides(eligiblePromotions, policyMode, quote);

        var selection = _selector.SelectPromotions(
            eligiblePromotions,
            lineMap,
            policyMode,
            groupOverrides,
            priceList.Currency);
        var couponsAllowed = selection.Selected.All(x => x.Campaign.CombinableWithCoupons);
        quote.Trace.Add(new QuoteTraceEntry("stacking", $"Selected {selection.Selected.Count} promotion(s) using {policyMode}."));

        foreach (var rejected in selection.Rejected)
        {
            var rejectedGroup = GetEffectiveStackingGroup(rejected.Campaign, quote, out _, logIfInferred: false);
            quote.RejectedSources.Add(new QuoteSourceResult(
                SourceTypePromotion,
                rejected.Campaign.Id.ToString(),
                rejected.Campaign.Name,
                false,
                rejected.Reason,
                rejected.Campaign.Priority,
                rejectedGroup));
        }

        var sourcePriorities = new Dictionary<string, int>();
        var guardrailsBySourceId = new Dictionary<string, Guardrails>();
        foreach (var promo in selection.Selected.OrderByDescending(x => x.Campaign.Priority))
        {
            ApplyPromotion(promo, lineMap, quote, priceList.Currency, guardrailsBySourceId);
            sourcePriorities[promo.Campaign.Id.ToString()] = promo.Campaign.Priority;
        }

        await ApplyCouponAsync(request, catalogInfo, batchInfo, timestamp, lineMap, quote, couponsAllowed, guardrailsBySourceId, ct);

        EnforceCaps(quote, guardrailsBySourceId);
        EnforceGuardrails(quote, catalogInfo, sourcePriorities);

        FinalizeTotals(quote);
        return quote;
    }

    private async Task<PriceList> LoadActivePriceListAsync(DateTime timestamp, CancellationToken ct)
    {
        var priceList = await _db.PriceLists
            .Include(x => x.Items)
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => (!x.ValidFrom.HasValue || x.ValidFrom.Value <= timestamp) && (!x.ValidTo.HasValue || x.ValidTo.Value >= timestamp))
            .OrderByDescending(x => x.ValidFrom ?? DateTime.MinValue)
            .FirstOrDefaultAsync(ct);

        if (priceList is null)
            throw new InvalidOperationException("No active price list found.");

        return priceList;
    }

    private static Dictionary<string, QuoteLine> BuildLines(
        QuoteRequest request,
        PriceList priceList,
        PriceQuote quote)
    {
        var lineMap = new Dictionary<string, QuoteLine>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            var priceItem = priceList.Items.FirstOrDefault(x => string.Equals(x.SkuId, item.SkuId, StringComparison.OrdinalIgnoreCase));
            if (priceItem is null)
                throw new InvalidOperationException($"Price list missing SKU {item.SkuId}.");

            if (lineMap.TryGetValue(item.SkuId, out var existing))
            {
                if (existing.BatchId != item.BatchId)
                    throw new InvalidOperationException($"SKU {item.SkuId} appears with multiple batch ids in the same quote.");

                existing.Quantity += item.Qty;
                existing.BaseUnitPrice = ResolveTierPrice(priceItem, existing.Quantity);
                existing.FinalUnitPrice = existing.BaseUnitPrice;
                continue;
            }

            var unitPrice = ResolveTierPrice(priceItem, item.Qty);
            var line = new QuoteLine
            {
                SkuId = item.SkuId,
                BatchId = item.BatchId,
                Quantity = item.Qty,
                BaseUnitPrice = unitPrice,
                FinalUnitPrice = unitPrice,
                IsGift = false
            };

            lineMap[item.SkuId] = line;
        }

        quote.Lines.Clear();
        quote.Lines.AddRange(lineMap.Values);
        return lineMap;
    }

    private static decimal ResolveTierPrice(PriceListItem item, int qty)
    {
        if (item.TierPrices.Count == 0) return item.BasePrice;

        var tier = item.TierPrices
            .Where(t => t.MinQty <= qty)
            .OrderByDescending(t => t.MinQty)
            .FirstOrDefault();

        return tier == default ? item.BasePrice : tier.UnitPrice;
    }

    private async Task ApplyOverridesAsync(
        QuoteRequest request,
        DateTime timestamp,
        PriceList priceList,
        IReadOnlyDictionary<string, QuoteLine> lineMap,
        PriceQuote quote,
        CancellationToken ct)
    {
        var skuIds = lineMap.Keys.ToList();

        var overridesQuery = _db.PriceOverrides
            .AsNoTracking()
            .Where(x => skuIds.Contains(x.SkuId))
            .Where(x => (!x.ValidFrom.HasValue || x.ValidFrom.Value <= timestamp) && (!x.ValidTo.HasValue || x.ValidTo.Value >= timestamp));

        if (request.UserId.HasValue)
        {
            var userId = request.UserId.Value;
            overridesQuery = overridesQuery.Where(x =>
                (x.ScopeType == OverrideScope.Site && x.ScopeId == request.SiteId) ||
                (x.ScopeType == OverrideScope.User && x.ScopeId == userId));
        }
        else
        {
            overridesQuery = overridesQuery.Where(x => x.ScopeType == OverrideScope.Site && x.ScopeId == request.SiteId);
        }

        var overrides = await overridesQuery.ToListAsync(ct);

        foreach (var line in lineMap.Values)
        {
            PriceOverride? selected = null;

            if (request.UserId.HasValue)
            {
                selected = overrides
                    .Where(x => x.ScopeType == OverrideScope.User && x.ScopeId == request.UserId.Value && string.Equals(x.SkuId, line.SkuId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Priority)
                    .FirstOrDefault();
            }

            if (selected is null)
            {
                selected = overrides
                    .Where(x => x.ScopeType == OverrideScope.Site && x.ScopeId == request.SiteId && string.Equals(x.SkuId, line.SkuId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.Priority)
                    .FirstOrDefault();
            }

            if (selected is null) continue;

            if (!string.Equals(selected.Currency, priceList.Currency, StringComparison.OrdinalIgnoreCase))
                continue;

            var before = line.FinalUnitPrice;
            var proposed = selected.OverrideType switch
            {
                OverrideType.PercentOff => RoundingRules.Round(before * (1m - selected.Value / 100m), priceList.Currency),
                OverrideType.AmountOff => RoundingRules.Round(Math.Max(0m, before - selected.Value), priceList.Currency),
                OverrideType.FixedPrice => RoundingRules.Round(selected.Value, priceList.Currency),
                _ => before
            };

            if (proposed >= before)
                continue;

            line.FinalUnitPrice = proposed;
            var delta = line.FinalUnitPrice - before;
            if (delta != 0m)
            {
                line.Adjustments.Add(new PriceAdjustment(SourceTypeOverride, selected.Id.ToString(), "Price override", delta));
                quote.AppliedSources.Add(new QuoteSourceResult(
                    SourceTypeOverride,
                    selected.Id.ToString(),
                    "Price override",
                    true,
                    null,
                    selected.Priority,
                    selected.StackingGroup));
            }
        }
    }

    private static void EnsureCatalogResolved(
        IEnumerable<string> skuIds,
        IDictionary<string, CatalogSkuInfo> catalog,
        PriceQuote quote)
    {
        var failures = new List<ValidationFailure>();
        foreach (var sku in skuIds)
        {
            if (catalog.ContainsKey(sku)) continue;
            quote.Trace.Add(new QuoteTraceEntry("catalog", $"Catalog metadata not resolved for SKU {sku}."));
            failures.Add(new ValidationFailure("items", $"Catalog metadata not resolved for SKU {sku}."));
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }

    private async Task<IReadOnlyDictionary<Guid, BatchInfo>> ValidateBatchesAsync(
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        CancellationToken ct)
    {
        var itemsWithBatch = request.Items
            .Where(i => i.BatchId.HasValue)
            .Select(i => new SkuBatchKey(i.SkuId, i.BatchId!.Value))
            .ToList();

        if (itemsWithBatch.Count == 0)
            return new Dictionary<Guid, BatchInfo>();

        var uniquePairs = itemsWithBatch
            .Distinct(new SkuBatchKeyComparer())
            .ToList();

        foreach (var sku in uniquePairs.Select(x => x.SkuId).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!catalog.TryGetValue(sku, out var info) || !info.IsBatchSelectable)
                throw new InvalidOperationException($"SKU {sku} is not batch selectable.");
        }

        var batches = new Dictionary<Guid, BatchInfo>();
        using var semaphore = new SemaphoreSlim(BatchValidationConcurrency, BatchValidationConcurrency);

        var tasks = uniquePairs.Select(async pair =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var batch = await _inventory.GetBatchInfoAsync(pair.SkuId, pair.BatchId, ct);
                if (batch is null)
                    throw new InvalidOperationException($"Batch {pair.BatchId} does not belong to SKU {pair.SkuId}.");
                return batch;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var resolved = await Task.WhenAll(tasks);
        foreach (var batch in resolved)
        {
            batches[batch.BatchId] = batch;
        }

        return batches;
    }

    private async Task<IReadOnlyList<EligiblePromotion>> LoadEligiblePromotionsAsync(
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp,
        PriceQuote quote,
        CancellationToken ct)
    {
        var campaigns = await _db.PromotionCampaigns
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => (!x.ValidFrom.HasValue || x.ValidFrom.Value <= timestamp) && (!x.ValidTo.HasValue || x.ValidTo.Value >= timestamp))
            .ToListAsync(ct);

        var eligible = new List<EligiblePromotion>();

        foreach (var campaign in campaigns)
        {
            var result = _eligibility.Evaluate(campaign.Eligibility, request, catalog, batches, timestamp);
            if (!result.IsEligible)
            {
                var rejectedGroup = GetEffectiveStackingGroup(campaign, quote, out _);
                quote.RejectedSources.Add(new QuoteSourceResult(
                    SourceTypePromotion,
                    campaign.Id.ToString(),
                    campaign.Name,
                    false,
                    result.Reason,
                    campaign.Priority,
                    rejectedGroup));
                continue;
            }

            var effectiveGroup = GetEffectiveStackingGroup(campaign, quote, out _);
            eligible.Add(new EligiblePromotion(campaign, result.EligibleSkuIds, effectiveGroup));
        }

        return eligible;
    }

    private void ApplyPromotion(
        EligiblePromotion promo,
        IReadOnlyDictionary<string, QuoteLine> lines,
        PriceQuote quote,
        string currency,
        IDictionary<string, Guardrails> guardrailsBySourceId)
    {
        if (promo.Campaign.Benefit is BundleFixedPriceBenefitDefinition bundle)
        {
            var requirements = _benefits.GetBundleRequirements(bundle);
            var bundleCount = _benefits.GetBundleCount(bundle, lines);
            if (bundleCount > 0)
            {
                var requirementText = requirements.Count == 0
                    ? "none"
                    : string.Join(", ", requirements.Select(r => $"{r.SkuId}x{r.QtyRequired}"));
                var participatingText = requirements.Count == 0
                    ? "none"
                    : string.Join(", ", requirements.Select(r => $"{r.SkuId}={bundleCount * r.QtyRequired}"));

                quote.Trace.Add(new QuoteTraceEntry(
                    "bundle",
                    $"Bundle {promo.Campaign.Name} applied {bundleCount} time(s) using [{requirementText}]. Participating quantities: {participatingText}. Discount averaged across line quantities."));
            }
        }

        var result = _benefits.ApplyBenefit(
            promo.Campaign.Benefit,
            lines,
            promo.EligibleSkuIds,
            SourceTypePromotion,
            promo.Campaign.Id.ToString(),
            promo.Campaign.Name,
            currency);

        if (result.GiftLines.Count > 0)
            quote.Lines.AddRange(result.GiftLines);

        if (result.DiscountTotal > 0m || result.CashbackTotal > 0m || result.GiftLines.Count > 0)
        {
            quote.CashbackTotal += result.CashbackTotal;
            quote.AppliedSources.Add(new QuoteSourceResult(
                SourceTypePromotion,
                promo.Campaign.Id.ToString(),
                promo.Campaign.Name,
                true,
                null,
                promo.Campaign.Priority,
                promo.EffectiveStackingGroup));

            if (promo.Campaign.Guardrails is not null)
                guardrailsBySourceId[promo.Campaign.Id.ToString()] = promo.Campaign.Guardrails;
        }
    }

    private async Task ApplyCouponAsync(
        QuoteRequest request,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<Guid, BatchInfo> batches,
        DateTime timestamp,
        IReadOnlyDictionary<string, QuoteLine> lines,
        PriceQuote quote,
        bool couponsAllowed,
        IDictionary<string, Guardrails> guardrailsBySourceId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.CouponCode)) return;

        var code = request.CouponCode.Trim().ToUpperInvariant();
        var coupon = await _db.Coupons.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code, ct);
        if (coupon is null)
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, code, code, false, "Coupon not found.", null, null));
            return;
        }

        if (!coupon.IsActive || (coupon.ValidFrom.HasValue && coupon.ValidFrom.Value > timestamp) || (coupon.ValidTo.HasValue && coupon.ValidTo.Value < timestamp))
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, "Coupon is inactive.", coupon.Priority, null));
            return;
        }

        if (coupon.MaxUsesPerUser.HasValue && !request.UserId.HasValue)
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, "Coupon requires a user.", coupon.Priority, null));
            return;
        }

        var usageStatus = await CheckCouponUsageAsync(coupon, request, timestamp, ct);
        if (!usageStatus.IsAllowed)
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, usageStatus.Reason, coupon.Priority, null));
            return;
        }

        quote.Trace.Add(new QuoteTraceEntry(
            "coupon",
            usageStatus.UsedReservation
                ? "Coupon reservation validated."
                : "Coupon usage limits will be enforced at checkout."));

        var eligibility = _eligibility.Evaluate(coupon.Eligibility, request, catalog, batches, timestamp);
        if (!eligibility.IsEligible)
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, eligibility.Reason, coupon.Priority, null));
            return;
        }

        if (!coupon.CombinableWithPromotions && quote.AppliedSources.Any(s => s.SourceType == SourceTypePromotion))
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, "Coupon not combinable with promotions.", coupon.Priority, null));
            return;
        }

        if (!couponsAllowed)
        {
            quote.RejectedSources.Add(new QuoteSourceResult(SourceTypeCoupon, coupon.Id.ToString(), coupon.Code, false, "Coupon blocked by promotion.", coupon.Priority, null));
            return;
        }

        var result = _benefits.ApplyBenefit(
            coupon.Benefit,
            lines,
            eligibility.EligibleSkuIds,
            SourceTypeCoupon,
            coupon.Id.ToString(),
            coupon.Code,
            quote.Currency);

        if (result.GiftLines.Count > 0)
            quote.Lines.AddRange(result.GiftLines);

        quote.CashbackTotal += result.CashbackTotal;
        quote.AppliedSources.Add(new QuoteSourceResult(
            SourceTypeCoupon,
            coupon.Id.ToString(),
            coupon.Code,
            true,
            null,
            coupon.Priority,
            null));

        if (coupon.Guardrails is not null)
            guardrailsBySourceId[coupon.Id.ToString()] = coupon.Guardrails;
    }

    private void EnforceGuardrails(
        PriceQuote quote,
        IReadOnlyDictionary<string, CatalogSkuInfo> catalog,
        IReadOnlyDictionary<string, int> sourcePriorities)
    {
        var floors = catalog.ToDictionary(k => k.Key, v => v.Value.MinAllowedPrice, StringComparer.OrdinalIgnoreCase);
        var actions = _guardrails.Enforce(quote, floors, sourcePriorities);

        var sourceReasons = new Dictionary<(string SourceType, string SourceId), string>();
        foreach (var action in actions)
        {
            if (action.SourceType == "promotion" || action.SourceType == "coupon")
            {
                sourceReasons.TryAdd((action.SourceType, action.SourceId), action.Reason);
            }

            quote.Trace.Add(new QuoteTraceEntry("guardrail", action.Reason));
        }

        foreach (var entry in sourceReasons)
        {
            var key = entry.Key;
            var stillApplied = quote.Lines.Any(l =>
                l.Adjustments.Any(a => a.SourceType == key.SourceType && a.SourceId == key.SourceId));

            if (stillApplied) continue;

            var applied = quote.AppliedSources.FirstOrDefault(s => s.SourceType == key.SourceType && s.SourceId == key.SourceId);
            quote.AppliedSources.RemoveAll(s => s.SourceType == key.SourceType && s.SourceId == key.SourceId);

            if (!quote.RejectedSources.Any(s => s.SourceType == key.SourceType && s.SourceId == key.SourceId))
            {
                quote.RejectedSources.Add(new QuoteSourceResult(
                    key.SourceType,
                    key.SourceId,
                    applied?.Name ?? key.SourceId,
                    false,
                    entry.Value,
                    applied?.Priority,
                    applied?.StackingGroup));
            }
        }
    }

    private sealed record CouponUsageStatus(bool IsAllowed, string Reason, bool UsedReservation);

    private async Task<CouponUsageStatus> CheckCouponUsageAsync(Coupon coupon, QuoteRequest request, DateTime timestamp, CancellationToken ct)
    {
        if (coupon.MaxUsesTotal is null && coupon.MaxUsesPerUser is null)
            return new CouponUsageStatus(true, string.Empty, false);

        var totalRedeemed = await _db.CouponRedemptions
            .AsNoTracking()
            .CountAsync(x => x.CouponId == coupon.Id && x.RedeemedAt != null, ct);

        var activeReservations = await _db.CouponRedemptions
            .AsNoTracking()
            .CountAsync(x => x.CouponId == coupon.Id && x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > timestamp, ct);

        var usedReservation = false;
        var userReservationCount = 0;

        if (request.UserId.HasValue)
        {
            usedReservation = await _db.CouponRedemptions
                .AsNoTracking()
                .AnyAsync(x => x.CouponId == coupon.Id && x.UserId == request.UserId && x.ReservationId != null && x.ExpiresAt.HasValue && x.ExpiresAt > timestamp && x.RedeemedAt == null, ct);
            userReservationCount = usedReservation ? 1 : 0;
        }

        if (coupon.MaxUsesPerUser.HasValue && request.UserId.HasValue)
        {
            var perUserRedeemed = await _db.CouponRedemptions
                .AsNoTracking()
                .CountAsync(x => x.CouponId == coupon.Id && x.UserId == request.UserId && x.RedeemedAt != null, ct);

            var perUserReserved = await _db.CouponRedemptions
                .AsNoTracking()
                .CountAsync(x => x.CouponId == coupon.Id && x.UserId == request.UserId && x.RedeemedAt == null && x.ExpiresAt.HasValue && x.ExpiresAt > timestamp, ct);

            if (perUserRedeemed + Math.Max(0, perUserReserved - userReservationCount) >= coupon.MaxUsesPerUser.Value)
                return new CouponUsageStatus(false, "Coupon usage limit reached for user.", false);
        }

        if (coupon.MaxUsesTotal.HasValue && totalRedeemed + Math.Max(0, activeReservations - userReservationCount) >= coupon.MaxUsesTotal.Value)
            return new CouponUsageStatus(false, "Coupon usage limit reached.", false);

        return new CouponUsageStatus(true, string.Empty, usedReservation);
    }

    private void EnforceCaps(PriceQuote quote, IReadOnlyDictionary<string, Guardrails> guardrailsBySourceId)
    {
        if (guardrailsBySourceId.Count == 0) return;

        var lineCaps = new Dictionary<string, LineCapContext>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in quote.Lines.Where(l => !l.IsGift))
        {
            var applicable = line.Adjustments
                .Where(a => (a.SourceType == SourceTypePromotion || a.SourceType == SourceTypeCoupon)
                            && guardrailsBySourceId.ContainsKey(a.SourceId))
                .ToList();

            if (applicable.Count == 0) continue;

            var guardrails = applicable
                .Select(a => guardrailsBySourceId[a.SourceId])
                .ToList();

            var percentCaps = guardrails.Where(g => g.MaxDiscountPercent.HasValue).Select(g => g.MaxDiscountPercent!.Value).ToList();
            var amountCaps = guardrails.Where(g => g.MaxDiscountAmount.HasValue).Select(g => g.MaxDiscountAmount!.Value).ToList();

            var effective = new Guardrails
            {
                MaxDiscountPercent = percentCaps.Count == 0 ? null : percentCaps.Min(),
                MaxDiscountAmount = amountCaps.Count == 0 ? null : amountCaps.Min()
            };

            var perUnitDiscount = applicable.Sum(a => Math.Max(0m, -a.Amount));
            lineCaps[line.SkuId] = new LineCapContext(effective, perUnitDiscount);
        }

        var actions = _guardrails.EnforceCaps(quote, lineCaps, quote.Currency);
        foreach (var action in actions)
        {
            quote.Trace.Add(new QuoteTraceEntry("guardrail", action.Reason));
        }
    }

    private async Task<StackingMode> LoadStackingModeAsync(Guid siteId, PriceQuote quote, CancellationToken ct)
    {
        var sitePolicy = await _db.PricingPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SiteId == siteId, ct);

        if (sitePolicy is not null)
        {
            quote.Trace.Add(new QuoteTraceEntry("stacking", $"Stacking mode {sitePolicy.DefaultStackingMode} from site policy."));
            return sitePolicy.DefaultStackingMode;
        }

        var globalPolicy = await _db.PricingPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SiteId == null, ct);

        if (globalPolicy is not null)
        {
            quote.Trace.Add(new QuoteTraceEntry("stacking", $"Stacking mode {globalPolicy.DefaultStackingMode} from global policy."));
            return globalPolicy.DefaultStackingMode;
        }

        quote.Trace.Add(new QuoteTraceEntry("stacking", $"Stacking mode {StackingPolicy.DefaultMode} from default policy."));
        return StackingPolicy.DefaultMode;
    }

    private static Dictionary<string, StackingMode> BuildGroupStackingOverrides(
        IReadOnlyList<EligiblePromotion> eligiblePromotions,
        StackingMode defaultMode,
        PriceQuote quote)
    {
        var overrides = new Dictionary<string, StackingMode>(StringComparer.OrdinalIgnoreCase);

        var grouped = eligiblePromotions.GroupBy(x => x.EffectiveStackingGroup ?? "default", StringComparer.OrdinalIgnoreCase);
        foreach (var group in grouped)
        {
            var overrideCampaign = group
                .Where(x => x.Campaign.StackingMode != defaultMode)
                .OrderByDescending(x => x.Campaign.Priority)
                .FirstOrDefault();

            if (overrideCampaign is null) continue;

            overrides[group.Key] = overrideCampaign.Campaign.StackingMode;
            quote.Trace.Add(new QuoteTraceEntry(
                "stacking",
                $"Group '{group.Key}' uses {overrideCampaign.Campaign.StackingMode} from campaign {overrideCampaign.Campaign.Name}."
            ));
        }

        return overrides;
    }

    private static string GetEffectiveStackingGroup(
        PromotionCampaign campaign,
        PriceQuote quote,
        out bool inferred,
        bool logIfInferred = true)
    {
        if (!string.IsNullOrWhiteSpace(campaign.StackingGroup))
        {
            inferred = false;
            return campaign.StackingGroup!;
        }

        inferred = true;
        var group = InferStackingGroup(campaign);
        if (logIfInferred)
        {
            quote.Trace.Add(new QuoteTraceEntry(
                "stacking",
                $"Campaign {campaign.Name} missing stacking group; inferred '{group}'."
            ));
        }

        return group;
    }

    private static string InferStackingGroup(PromotionCampaign campaign)
    {
        var benefitKind = campaign.Benefit.Kind;
        return benefitKind switch
        {
            "bundleFixedPrice" => "Bundle",
            "buyXGetY" => "Bundle",
            "cashbackPercent" => "Cashback",
            "cashbackAmount" => "Cashback",
            _ => campaign.Eligibility.Kind switch
            {
                "category" => "Category",
                "product" => "Product",
                "seasonal" => "Seasonal",
                "brand" => "Brand",
                _ => "Discount"
            }
        };
    }

    private static void FinalizeTotals(PriceQuote quote)
    {
        quote.Subtotal = quote.Lines.Where(l => !l.IsGift).Sum(l => l.BaseUnitPrice * l.Quantity);
        quote.FinalTotal = quote.Lines.Where(l => !l.IsGift).Sum(l => l.FinalUnitPrice * l.Quantity);
        quote.DiscountTotal = Math.Max(0m, quote.Subtotal - quote.FinalTotal);
    }

    private sealed record SkuBatchKey(string SkuId, Guid BatchId);

    private sealed class SkuBatchKeyComparer : IEqualityComparer<SkuBatchKey>
    {
        public bool Equals(SkuBatchKey? x, SkuBatchKey? y)
        {
            if (x is null || y is null) return false;
            return x.BatchId == y.BatchId && string.Equals(x.SkuId, y.SkuId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(SkuBatchKey obj)
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SkuId),
                obj.BatchId);
        }
    }
}
