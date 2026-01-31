using MediatR;
using Microsoft.EntityFrameworkCore;
using Pricing.Domain.Overrides;

namespace Pricing.Application.Features.Overrides;

public sealed record UpdatePriceOverrideCommand(
    Guid Id,
    OverrideType OverrideType,
    decimal Value,
    string Currency,
    DateTime? ValidFrom,
    DateTime? ValidTo,
    int Priority,
    string? StackingGroup) : IRequest;

public sealed class UpdatePriceOverrideHandler : IRequestHandler<UpdatePriceOverrideCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public UpdatePriceOverrideHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(UpdatePriceOverrideCommand request, CancellationToken ct)
    {
        var entity = await _db.PriceOverrides.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) throw new InvalidOperationException("Price override not found.");

        entity.Update(
            request.OverrideType,
            request.Value,
            request.Currency,
            request.ValidFrom,
            request.ValidTo,
            request.Priority,
            request.StackingGroup);

        await _db.SaveChangesAsync(ct);
    }
}

public sealed record DeletePriceOverrideCommand(Guid Id) : IRequest;

public sealed class DeletePriceOverrideHandler : IRequestHandler<DeletePriceOverrideCommand>
{
    private readonly Abstractions.IPricingDbContext _db;

    public DeletePriceOverrideHandler(Abstractions.IPricingDbContext db) => _db = db;

    public async Task Handle(DeletePriceOverrideCommand request, CancellationToken ct)
    {
        var entity = await _db.PriceOverrides.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (entity is null) return;

        _db.PriceOverrides.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
}
