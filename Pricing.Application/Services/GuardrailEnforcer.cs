using Pricing.Domain.Common;
using Pricing.Domain.Quotes;

namespace Pricing.Application.Services;

public sealed record GuardrailAction(string SourceType, string SourceId, string Reason);
public sealed record LineCapContext(Guardrails Guardrails, decimal PerUnitDiscount);

public sealed class GuardrailEnforcer
{
    public IReadOnlyList<GuardrailAction> Enforce(
        PriceQuote quote,
        IReadOnlyDictionary<string, decimal?> minPrices,
        IReadOnlyDictionary<string, int> sourcePriorities)
    {
        var actions = new List<GuardrailAction>();

        foreach (var line in quote.Lines.Where(l => !l.IsGift))
        {
            if (!minPrices.TryGetValue(line.SkuId, out var floor) || !floor.HasValue)
                continue;

            if (line.FinalUnitPrice >= floor.Value) continue;

            RemoveAdjustments(line, actions, sourcePriorities, "coupon");

            if (line.FinalUnitPrice >= floor.Value) continue;

            RemovePromotionsByPriority(line, actions, sourcePriorities, floor.Value);

            if (line.FinalUnitPrice < floor.Value)
            {
                var delta = floor.Value - line.FinalUnitPrice;
                line.FinalUnitPrice = floor.Value;
                line.Adjustments.Add(new PriceAdjustment("guardrail", "floor", "Clamped to price floor", delta));
                actions.Add(new GuardrailAction("guardrail", "floor", "Clamped to price floor."));
            }
        }

        return actions;
    }

    public IReadOnlyList<GuardrailAction> EnforceCaps(
        PriceQuote quote,
        IReadOnlyDictionary<string, LineCapContext> lineCaps,
        string currency)
    {
        var actions = new List<GuardrailAction>();

        foreach (var line in quote.Lines.Where(l => !l.IsGift))
        {
            if (!lineCaps.TryGetValue(line.SkuId, out var context)) continue;
            var discount = context.PerUnitDiscount;
            if (discount <= 0) continue;
            if (line.Quantity <= 0) continue;

            var totalDiscount = discount * line.Quantity;

            var maxByPercent = context.Guardrails.MaxDiscountPercent.HasValue
                ? line.BaseUnitPrice * line.Quantity * context.Guardrails.MaxDiscountPercent.Value / 100m
                : (decimal?)null;

            var maxByAmount = context.Guardrails.MaxDiscountAmount;
            var maxAllowed = new[] { maxByPercent, maxByAmount }
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .DefaultIfEmpty(decimal.MaxValue)
                .Min();

            if (totalDiscount > maxAllowed)
            {
                var deltaTotal = totalDiscount - maxAllowed;
                var deltaPerUnit = deltaTotal / line.Quantity;

                line.FinalUnitPrice = RoundingRules.Round(line.FinalUnitPrice + deltaPerUnit, currency);
                line.Adjustments.Add(new PriceAdjustment("guardrail", "cap", $"Clamped to discount cap for {line.SkuId}", deltaPerUnit));
                actions.Add(new GuardrailAction("guardrail", "cap", $"Discount capped for SKU {line.SkuId}."));
            }
        }

        return actions;
    }

    private static void RemoveAdjustments(
        QuoteLine line,
        List<GuardrailAction> actions,
        IReadOnlyDictionary<string, int> sourcePriorities,
        string sourceType)
    {
        var toRemove = line.Adjustments
            .Where(a => string.Equals(a.SourceType, sourceType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (toRemove.Count == 0) return;

        foreach (var adjustment in toRemove)
        {
            line.Adjustments.Remove(adjustment);
            line.FinalUnitPrice -= adjustment.Amount;
            actions.Add(new GuardrailAction(sourceType, adjustment.SourceId, "Removed due to price floor."));
        }
    }

    private static void RemovePromotionsByPriority(
        QuoteLine line,
        List<GuardrailAction> actions,
        IReadOnlyDictionary<string, int> sourcePriorities,
        decimal floor)
    {
        var promotions = line.Adjustments
            .Where(a => string.Equals(a.SourceType, "promotion", StringComparison.OrdinalIgnoreCase))
            .GroupBy(a => a.SourceId)
            .Select(g => new
            {
                SourceId = g.Key,
                Adjustments = g.ToList(),
                Priority = sourcePriorities.TryGetValue(g.Key, out var p) ? p : int.MinValue
            })
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var promo in promotions)
        {
            foreach (var adjustment in promo.Adjustments)
            {
                line.Adjustments.Remove(adjustment);
                line.FinalUnitPrice -= adjustment.Amount;
            }

            actions.Add(new GuardrailAction("promotion", promo.SourceId, "Removed due to price floor."));

            if (line.FinalUnitPrice >= floor)
                break;
        }
    }
}
