using Pricing.Domain.Common;
using Pricing.Domain.Promotions.Definitions;
using Pricing.Domain.Quotes;

namespace Pricing.Application.Services;

public sealed class BenefitApplier
{
    public decimal EstimateDiscount(
        BenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string currency)
    {
        return benefit switch
        {
            PercentOffBenefitDefinition percent => EstimatePercent(percent, lines, eligibleSkus),
            AmountOffBenefitDefinition amount => EstimateAmount(amount, lines, eligibleSkus, currency),
            FixedPriceBenefitDefinition fixedPrice => EstimateFixedPrice(fixedPrice, lines, eligibleSkus, currency),
            BundleFixedPriceBenefitDefinition bundle => EstimateBundle(bundle, lines, currency),
            BuyXGetYBenefitDefinition => 0m,
            CashbackPercentBenefitDefinition => 0m,
            CashbackAmountBenefitDefinition => 0m,
            _ => 0m
        };
    }

    public BenefitApplicationResult ApplyBenefit(
        BenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string sourceType,
        string sourceId,
        string sourceName,
        string currency)
    {
        return benefit switch
        {
            PercentOffBenefitDefinition percent => ApplyPercent(percent, lines, eligibleSkus, sourceType, sourceId, sourceName, currency),
            AmountOffBenefitDefinition amount => ApplyAmount(amount, lines, eligibleSkus, sourceType, sourceId, sourceName, currency),
            FixedPriceBenefitDefinition fixedPrice => ApplyFixedPrice(fixedPrice, lines, eligibleSkus, sourceType, sourceId, sourceName, currency),
            BundleFixedPriceBenefitDefinition bundle => ApplyBundle(bundle, lines, sourceType, sourceId, sourceName, currency),
            BuyXGetYBenefitDefinition buyXGetY => ApplyBuyXGetY(buyXGetY, lines, sourceType, sourceId, sourceName),
            CashbackPercentBenefitDefinition cashbackPercent => ApplyCashbackPercent(cashbackPercent, lines, eligibleSkus),
            CashbackAmountBenefitDefinition cashbackAmount => ApplyCashbackAmount(cashbackAmount, lines, eligibleSkus, currency),
            _ => new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>())
        };
    }

    private static decimal EstimatePercent(
        PercentOffBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus)
    {
        var total = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            total += line.FinalUnitPrice * line.Quantity * benefit.Percent / 100m;
        }
        return total;
    }

    private static decimal EstimateAmount(
        AmountOffBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency)) return 0m;

        var total = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            total += benefit.Amount * line.Quantity;
        }
        return total;
    }

    private static decimal EstimateFixedPrice(
        FixedPriceBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency)) return 0m;

        var total = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            var current = line.FinalUnitPrice;
            if (current > benefit.Price)
                total += (current - benefit.Price) * line.Quantity;
        }
        return total;
    }

    public IReadOnlyList<BundleItemRequirement> GetBundleRequirements(BundleFixedPriceBenefitDefinition benefit)
    {
        if (benefit.RequiredItems is { Count: > 0 })
            return benefit.RequiredItems;

        if (benefit.SkuIds is { Count: > 0 })
            return benefit.SkuIds.Select(sku => new BundleItemRequirement(sku, 1)).ToArray();

        return Array.Empty<BundleItemRequirement>();
    }

    public int GetBundleCount(
        BundleFixedPriceBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines)
    {
        var requirements = GetBundleRequirements(benefit);
        if (requirements.Count == 0) return 0;

        var counts = new List<int>(requirements.Count);
        foreach (var requirement in requirements)
        {
            if (requirement.QtyRequired <= 0) return 0;
            if (!lines.TryGetValue(requirement.SkuId, out var line)) return 0;
            counts.Add(line.Quantity / requirement.QtyRequired);
        }

        return counts.Min();
    }

    private decimal EstimateBundle(
        BundleFixedPriceBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency)) return 0m;
        var requirements = GetBundleRequirements(benefit);
        if (requirements.Count == 0) return 0m;
        var bundleCount = GetBundleCount(benefit, lines);
        if (bundleCount <= 0) return 0m;

        var total = 0m;
        foreach (var requirement in requirements)
        {
            var line = lines[requirement.SkuId];
            total += line.FinalUnitPrice * requirement.QtyRequired * bundleCount;
        }

        var bundlePriceTotal = benefit.BundlePrice * bundleCount;
        return total > bundlePriceTotal ? total - bundlePriceTotal : 0m;
    }

    private static BenefitApplicationResult ApplyPercent(
        PercentOffBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string sourceType,
        string sourceId,
        string sourceName,
        string currency)
    {
        var discountTotal = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;

            var before = line.FinalUnitPrice;
            var discount = before * benefit.Percent / 100m;
            discount = Math.Min(discount, before);
            var newPrice = RoundingRules.Round(before - discount, currency);
            line.FinalUnitPrice = newPrice;
            var actualDiscount = before - newPrice;
            discountTotal += actualDiscount * line.Quantity;
            line.Adjustments.Add(new PriceAdjustment(sourceType, sourceId, sourceName, -actualDiscount));
        }

        return new BenefitApplicationResult(discountTotal, 0m, Array.Empty<QuoteLine>());
    }

    private static BenefitApplicationResult ApplyAmount(
        AmountOffBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string sourceType,
        string sourceId,
        string sourceName,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency))
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var discountTotal = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            var before = line.FinalUnitPrice;
            var discount = Math.Min(benefit.Amount, before);
            var newPrice = RoundingRules.Round(before - discount, currency);
            line.FinalUnitPrice = newPrice;
            var actualDiscount = before - newPrice;
            discountTotal += actualDiscount * line.Quantity;
            line.Adjustments.Add(new PriceAdjustment(sourceType, sourceId, sourceName, -actualDiscount));
        }

        return new BenefitApplicationResult(discountTotal, 0m, Array.Empty<QuoteLine>());
    }

    private static BenefitApplicationResult ApplyFixedPrice(
        FixedPriceBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string sourceType,
        string sourceId,
        string sourceName,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency))
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var discountTotal = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            if (line.FinalUnitPrice <= benefit.Price) continue;

            var before = line.FinalUnitPrice;
            var newPrice = RoundingRules.Round(benefit.Price, currency);
            line.FinalUnitPrice = newPrice;
            var actualDiscount = before - newPrice;
            discountTotal += actualDiscount * line.Quantity;
            line.Adjustments.Add(new PriceAdjustment(sourceType, sourceId, sourceName, -actualDiscount));
        }

        return new BenefitApplicationResult(discountTotal, 0m, Array.Empty<QuoteLine>());
    }

    private BenefitApplicationResult ApplyBundle(
        BundleFixedPriceBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string sourceType,
        string sourceId,
        string sourceName,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency))
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var requirements = GetBundleRequirements(benefit);
        if (requirements.Count == 0)
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var bundleCount = GetBundleCount(benefit, lines);
        if (bundleCount <= 0) return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var bundleLines = requirements
            .Select(r => new BundleLine(lines[r.SkuId], r.QtyRequired))
            .ToList();

        var total = bundleLines.Sum(l => l.Line.FinalUnitPrice * l.RequiredQty * bundleCount);
        var bundlePriceTotal = benefit.BundlePrice * bundleCount;
        if (total <= bundlePriceTotal) return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var discountTotal = total - bundlePriceTotal;
        var beforeTotal = 0m;
        var afterTotal = 0m;
        foreach (var bundleLine in bundleLines)
        {
            var participatingQty = bundleLine.RequiredQty * bundleCount;
            beforeTotal += bundleLine.Line.FinalUnitPrice * participatingQty;
            var lineTotal = bundleLine.Line.FinalUnitPrice * participatingQty;
            var share = lineTotal / total;
            var lineDiscountTotal = discountTotal * share;
            var scopedPerUnitDiscount = participatingQty == 0 ? 0m : lineDiscountTotal / participatingQty;
            var effectivePerUnitDiscount = bundleLine.Line.Quantity == 0
                ? 0m
                : scopedPerUnitDiscount * participatingQty / bundleLine.Line.Quantity;
            var oldPrice = bundleLine.Line.FinalUnitPrice;
            var newPriceRaw = oldPrice - effectivePerUnitDiscount;
            var newPrice = RoundingRules.Round(Math.Max(0m, newPriceRaw), currency);
            bundleLine.Line.FinalUnitPrice = newPrice;
            var actualPerUnitDiscount = oldPrice - newPrice;
            afterTotal += bundleLine.Line.FinalUnitPrice * participatingQty;
            bundleLine.Line.Adjustments.Add(new PriceAdjustment(sourceType, sourceId, sourceName, -actualPerUnitDiscount));
        }

        discountTotal = Math.Max(0m, beforeTotal - afterTotal);
        return new BenefitApplicationResult(discountTotal, 0m, Array.Empty<QuoteLine>());
    }

    private static BenefitApplicationResult ApplyBuyXGetY(
        BuyXGetYBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string sourceType,
        string sourceId,
        string sourceName)
    {
        if (!lines.TryGetValue(benefit.BuySkuId, out var buyLine))
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        if (benefit.BuyQty <= 0 || benefit.GetQty <= 0)
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var bundles = buyLine.Quantity / benefit.BuyQty;
        if (bundles <= 0) return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var giftQty = bundles * benefit.GetQty;
        var giftLine = new QuoteLine
        {
            SkuId = benefit.GetSkuId,
            Quantity = giftQty,
            BaseUnitPrice = 0m,
            FinalUnitPrice = 0m,
            IsGift = true
        };

        giftLine.Adjustments.Add(new PriceAdjustment(sourceType, sourceId, sourceName, 0m));
        return new BenefitApplicationResult(0m, 0m, new[] { giftLine });
    }

    private static BenefitApplicationResult ApplyCashbackPercent(
        CashbackPercentBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus)
    {
        var total = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            total += line.FinalUnitPrice * line.Quantity * benefit.Percent / 100m;
        }

        return new BenefitApplicationResult(0m, total, Array.Empty<QuoteLine>());
    }

    private static BenefitApplicationResult ApplyCashbackAmount(
        CashbackAmountBenefitDefinition benefit,
        IReadOnlyDictionary<string, QuoteLine> lines,
        IReadOnlySet<string> eligibleSkus,
        string currency)
    {
        if (!IsCurrencyMatch(benefit.Currency, currency))
            return new BenefitApplicationResult(0m, 0m, Array.Empty<QuoteLine>());

        var total = 0m;
        foreach (var sku in eligibleSkus)
        {
            if (!lines.TryGetValue(sku, out var line)) continue;
            total += benefit.Amount * line.Quantity;
        }

        return new BenefitApplicationResult(0m, total, Array.Empty<QuoteLine>());
    }

    private static bool IsCurrencyMatch(string? benefitCurrency, string currency)
    {
        return string.IsNullOrWhiteSpace(benefitCurrency)
            || string.Equals(benefitCurrency, currency, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record BundleLine(QuoteLine Line, int RequiredQty);
}
