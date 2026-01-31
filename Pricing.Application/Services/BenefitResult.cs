using Pricing.Domain.Quotes;

namespace Pricing.Application.Services;

public sealed record BenefitApplicationResult(
    decimal DiscountTotal,
    decimal CashbackTotal,
    IReadOnlyList<QuoteLine> GiftLines);
