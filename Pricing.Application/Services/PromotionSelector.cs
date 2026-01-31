using Pricing.Domain.Promotions;
using Pricing.Domain.Common;
using Pricing.Domain.Quotes;

namespace Pricing.Application.Services;

public sealed class PromotionSelector
{
    private readonly BenefitApplier _benefits;

    public PromotionSelector(BenefitApplier benefits) => _benefits = benefits;

    public PromotionSelectionResult SelectPromotions(
        IReadOnlyList<EligiblePromotion> eligible,
        IReadOnlyDictionary<string, QuoteLine> lines,
        StackingMode defaultMode,
        IReadOnlyDictionary<string, StackingMode> groupModeOverrides,
        string currency)
    {
        var rejected = new List<RejectedPromotion>();
        if (eligible.Count == 0)
            return new PromotionSelectionResult(Array.Empty<EligiblePromotion>(), Array.Empty<RejectedPromotion>());

        var remaining = ApplyExclusiveGroups(eligible, rejected);

        var grouped = remaining
            .GroupBy(x => x.EffectiveStackingGroup ?? "default", StringComparer.OrdinalIgnoreCase)
            .ToList();

        var groupSelected = new List<EligiblePromotion>();
        foreach (var group in grouped)
        {
            var mode = groupModeOverrides.TryGetValue(group.Key, out var overrideMode)
                ? overrideMode
                : defaultMode;

            var selectedInGroup = SelectByMode(group.ToList(), lines, currency, mode);
            groupSelected.AddRange(selectedInGroup);

            foreach (var promo in group)
            {
                if (!selectedInGroup.Contains(promo))
                    rejected.Add(new RejectedPromotion(promo.Campaign, $"Not selected by {mode} stacking mode in group '{group.Key}'."));
            }
        }

        IReadOnlyList<EligiblePromotion> selected = groupSelected;

        if (defaultMode != StackingMode.BestOfEachGroup)
        {
            var finalSelected = SelectByMode(selected, lines, currency, defaultMode);
            foreach (var promo in selected)
            {
                if (!finalSelected.Contains(promo))
                    rejected.Add(new RejectedPromotion(promo.Campaign, $"Not selected by {defaultMode} stacking mode."));
            }

            selected = finalSelected;
        }

        var nonCombinable = selected.Where(x => !x.Campaign.CombinableWithOtherPromotions)
            .OrderByDescending(x => x.Campaign.Priority)
            .FirstOrDefault();

        if (nonCombinable is not null)
        {
            foreach (var promo in selected.Where(x => x.Campaign.Id != nonCombinable.Campaign.Id))
                rejected.Add(new RejectedPromotion(promo.Campaign, "Not combinable with other promotions."));

            selected = new[] { nonCombinable };
        }

        var selectedIds = selected.Select(x => x.Campaign.Id).ToHashSet();
        foreach (var promo in remaining)
        {
            if (!selectedIds.Contains(promo.Campaign.Id))
                rejected.Add(new RejectedPromotion(promo.Campaign, "Not selected by stacking policy."));
        }

        return new PromotionSelectionResult(selected, rejected);
    }

    private static IReadOnlyList<EligiblePromotion> ApplyExclusiveGroups(
        IReadOnlyList<EligiblePromotion> eligible,
        List<RejectedPromotion> rejected)
    {
        var byGroup = eligible.Where(e => !string.IsNullOrWhiteSpace(e.Campaign.ExclusiveGroup))
            .GroupBy(e => e.Campaign.ExclusiveGroup!, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var remaining = eligible.ToList();
        foreach (var group in byGroup)
        {
            var winner = group.OrderByDescending(x => x.Campaign.Priority).First();
            foreach (var loser in group.Where(x => x.Campaign.Id != winner.Campaign.Id))
            {
                rejected.Add(new RejectedPromotion(loser.Campaign, "Excluded by exclusive group."));
                remaining.Remove(loser);
            }
        }

        return remaining;
    }

    private IReadOnlyList<EligiblePromotion> SelectByMode(
        IReadOnlyList<EligiblePromotion> eligible,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string currency,
        StackingMode mode)
    {
        if (eligible.Count == 0) return Array.Empty<EligiblePromotion>();

        return mode switch
        {
            StackingMode.PriorityOnly => SelectPriorityOnly(eligible),
            StackingMode.BestPrice => SelectBestPrice(eligible, lines, currency),
            StackingMode.Cascading => SelectCascading(eligible),
            _ => SelectBestOfEachGroup(eligible, lines, currency)
        };
    }

    private IReadOnlyList<EligiblePromotion> SelectPriorityOnly(IReadOnlyList<EligiblePromotion> eligible)
    {
        var winner = eligible.OrderByDescending(x => x.Campaign.Priority).First();
        return new[] { winner };
    }

    private IReadOnlyList<EligiblePromotion> SelectBestPrice(
        IReadOnlyList<EligiblePromotion> eligible,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string currency)
    {
        var best = eligible
            .OrderByDescending(x => _benefits.EstimateDiscount(x.Campaign.Benefit, lines, x.EligibleSkuIds, currency))
            .First();

        return new[] { best };
    }

    private IReadOnlyList<EligiblePromotion> SelectCascading(IReadOnlyList<EligiblePromotion> eligible)
    {
        var ordered = eligible.OrderByDescending(x => x.Campaign.Priority).ToList();
        var selected = new List<EligiblePromotion>();

        foreach (var promo in ordered)
        {
            if (selected.Count == 0)
            {
                selected.Add(promo);
                if (!promo.Campaign.CombinableWithOtherPromotions) break;
                continue;
            }

            if (!selected.Last().Campaign.CombinableWithOtherPromotions)
                break;

            selected.Add(promo);
            if (!promo.Campaign.CombinableWithOtherPromotions) break;
        }

        return selected;
    }

    private IReadOnlyList<EligiblePromotion> SelectBestOfEachGroup(
        IReadOnlyList<EligiblePromotion> eligible,
        IReadOnlyDictionary<string, QuoteLine> lines,
        string currency)
    {
        var grouped = eligible.GroupBy(x => x.EffectiveStackingGroup ?? "default", StringComparer.OrdinalIgnoreCase);
        var selected = new List<EligiblePromotion>();

        foreach (var group in grouped)
        {
            var best = group
                .OrderByDescending(x => _benefits.EstimateDiscount(x.Campaign.Benefit, lines, x.EligibleSkuIds, currency))
                .ThenByDescending(x => x.Campaign.Priority)
                .First();

            selected.Add(best);
        }

        return selected;
    }
}
