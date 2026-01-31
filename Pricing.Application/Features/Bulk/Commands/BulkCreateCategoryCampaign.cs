using MediatR;
using Pricing.Domain.Common;
using Pricing.Domain.Promotions;
using Pricing.Domain.Promotions.Definitions;

namespace Pricing.Application.Features.Bulk.Commands;

public sealed record BulkCreateCategoryCampaignCommand(
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<CategoryNameRef>? Categories,
    string NamePrefix,
    bool IsActive,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int Priority,
    string? StackingGroup,
    StackingMode StackingMode,
    bool CombinableWithOtherPromotions,
    bool CombinableWithCoupons,
    string? ExclusiveGroup,
    Guardrails? Guardrails,
    BenefitDefinition Benefit) : IRequest<int>;

public sealed record CategoryNameRef(Guid Id, string Name);

public sealed class BulkCreateCategoryCampaignHandler : IRequestHandler<BulkCreateCategoryCampaignCommand, int>
{
    private readonly Abstractions.IPricingDbContext _db;

    public BulkCreateCategoryCampaignHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<int> Handle(BulkCreateCategoryCampaignCommand request, CancellationToken ct)
    {
        if (request.CategoryIds.Count == 0) return 0;

        var categoryNameById = request.Categories is { Count: > 0 }
            ? request.Categories
                .GroupBy(x => x.Id)
                .ToDictionary(g => g.Key, g => g.First().Name)
            : new Dictionary<Guid, string>();

        var stackingGroup = string.IsNullOrWhiteSpace(request.StackingGroup)
            ? "Category"
            : request.StackingGroup;

        foreach (var categoryId in request.CategoryIds)
        {
            var eligibility = new CategoryEligibilityDefinition
            {
                CategoryIds = new[] { categoryId }
            };

            var categoryName = categoryNameById.TryGetValue(categoryId, out var n) ? n : null;
            var suffix = string.IsNullOrWhiteSpace(categoryName) ? categoryId.ToString() : categoryName!.Trim();

            var entity = PromotionCampaign.Create(
                name: $"{request.NamePrefix} {suffix}",
                isActive: request.IsActive,
                validFrom: request.ValidFrom,
                validTo: request.ValidTo,
                priority: request.Priority,
                stackingGroup: stackingGroup,
                stackingMode: request.StackingMode,
                combinableWithOtherPromotions: request.CombinableWithOtherPromotions,
                combinableWithCoupons: request.CombinableWithCoupons,
                exclusiveGroup: request.ExclusiveGroup,
                guardrails: request.Guardrails,
                eligibility: eligibility,
                benefit: request.Benefit);

            _db.PromotionCampaigns.Add(entity);
        }

        return await _db.SaveChangesAsync(ct);
    }
}
