using MediatR;
using Pricing.Application.Features.Quotes.Models;
using Pricing.Application.Services;
using Pricing.Domain.Quotes;

namespace Pricing.Application.Features.Quotes.Commands;

public sealed class CreateQuoteHandler : IRequestHandler<CreateQuoteCommand, QuoteResponse>
{
    private readonly PricingEngine _engine;

    public CreateQuoteHandler(PricingEngine engine) => _engine = engine;

    public async Task<QuoteResponse> Handle(CreateQuoteCommand request, CancellationToken ct)
    {
        var quote = await _engine.CreateQuoteAsync(request.Request, ct);
        return Map(quote);
    }

    private static QuoteResponse Map(PriceQuote quote)
    {
        return new QuoteResponse
        {
            Id = quote.Id,
            SiteId = quote.SiteId,
            UserId = quote.UserId,
            Currency = quote.Currency,
            Timestamp = quote.Timestamp,
            Subtotal = quote.Subtotal,
            DiscountTotal = quote.DiscountTotal,
            FinalTotal = quote.FinalTotal,
            CashbackTotal = quote.CashbackTotal,
            AppliedSources = quote.AppliedSources,
            RejectedSources = quote.RejectedSources,
            Trace = quote.Trace,
            Lines = quote.Lines.Select(l => new QuoteLineResponse
            {
                SkuId = l.SkuId,
                BatchId = l.BatchId,
                Quantity = l.Quantity,
                BaseUnitPrice = l.BaseUnitPrice,
                FinalUnitPrice = l.FinalUnitPrice,
                IsGift = l.IsGift,
                Adjustments = l.Adjustments.ToList()
            }).ToList()
        };
    }
}
