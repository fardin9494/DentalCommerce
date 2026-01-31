using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Pricing.Application.Features.PriceLists;

public sealed record DeletePriceListCommand(Guid Id) : IRequest;

public sealed class DeletePriceListHandler : IRequestHandler<DeletePriceListCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public DeletePriceListHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(DeletePriceListCommand request, CancellationToken ct)
    {
        var entity = await _db.PriceLists.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return;

        _db.PriceLists.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
