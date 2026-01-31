using MediatR;
using Pricing.Domain.Overrides;

namespace Pricing.Application.Features.Overrides;

public sealed record CreatePriceOverrideCommand(
    OverrideScope ScopeType,
    Guid ScopeId,
    string SkuId,
    OverrideType OverrideType,
    decimal Value,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int Priority,
    string? StackingGroup) : IRequest<Guid>;

public sealed class CreatePriceOverrideHandler : IRequestHandler<CreatePriceOverrideCommand, Guid>
{
    private readonly Abstractions.IPricingDbContext _db;

    public CreatePriceOverrideHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<Guid> Handle(CreatePriceOverrideCommand request, CancellationToken ct)
    {
        var entity = PriceOverride.Create(
            request.ScopeType,
            request.ScopeId,
            request.SkuId,
            request.OverrideType,
            request.Value,
            request.Currency,
            request.ValidFrom,
            request.ValidTo,
            request.Priority,
            request.StackingGroup);

        _db.PriceOverrides.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
