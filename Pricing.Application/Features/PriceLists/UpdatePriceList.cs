using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Pricing.Application.Features.PriceLists;

public sealed record UpdatePriceListCommand(
    Guid Id,
    string Name,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    bool IsActive) : IRequest;

public sealed class UpdatePriceListHandler : IRequestHandler<UpdatePriceListCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public UpdatePriceListHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(UpdatePriceListCommand request, CancellationToken ct)
    {
        var entity = await _db.PriceLists.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) throw new InvalidOperationException("Price list not found.");

        entity.Update(request.Name, request.Currency, request.ValidFrom, request.ValidTo, request.IsActive);
        await _db.SaveChangesAsync(ct);
    }
}
