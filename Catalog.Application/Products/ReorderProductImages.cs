using MediatR;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed record ReorderProductImagesCommand(Guid ProductId, IReadOnlyList<Guid> OrderedIds) : IRequest<Unit>;

public sealed class ReorderProductImagesHandler : IRequestHandler<ReorderProductImagesCommand, Unit>
{
    private readonly DbContext _db;
    public ReorderProductImagesHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(ReorderProductImagesCommand req, CancellationToken ct)
    {
        var imgs = await _db.Set<ProductImage>()
            .Where(i => i.ProductId == req.ProductId)
            .ToListAsync(ct);

        var allIds = imgs.Select(i => i.Id).ToHashSet();
        if (!req.OrderedIds.All(allIds.Contains) || allIds.Count != req.OrderedIds.Count)
            throw new InvalidOperationException("OrderedIds mismatch with existing images.");

        int order = 1;
        foreach (var id in req.OrderedIds)
        {
            var img = imgs.First(x => x.Id == id);
            _db.Entry(img).Property(nameof(ProductImage.SortOrder)).CurrentValue = order++;
        }
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}