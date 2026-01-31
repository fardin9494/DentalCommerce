using MediatR;
using Pricing.Application.Features.Quotes.Models;

namespace Pricing.Application.Features.Quotes.Commands;

public sealed record CreateQuoteCommand(QuoteRequest Request) : IRequest<QuoteResponse>;
