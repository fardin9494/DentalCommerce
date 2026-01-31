using MediatR;
using Pricing.Domain.PriceLists;

namespace Pricing.Application.Features.PriceLists;

public sealed record CreatePriceListCommand(
    string Name,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive) : IRequest<Guid>;

public sealed class CreatePriceListHandler : IRequestHandler<CreatePriceListCommand, Guid>
{
    private readonly Abstractions.IPricingDbContext _db;

    public CreatePriceListHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task<Guid> Handle(CreatePriceListCommand request, CancellationToken ct)
    {
        var entity = PriceList.Create(request.Name, request.Currency, request.ValidFrom, request.ValidTo, request.IsActive);
        _db.PriceLists.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
