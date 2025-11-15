using Catalog.Application.Categories;
using Catalog.Domain.Products;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed record SetProductCategoriesCommand(
    Guid ProductId,
    IReadOnlyList<Guid> CategoryIds,
    Guid? PrimaryCategoryId
) : IRequest<Unit>;

public sealed class SetProductCategoriesValidator : AbstractValidator<SetProductCategoriesCommand>
{
    public SetProductCategoriesValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.CategoryIds).NotNull();
    }
}

public sealed class SetProductCategoriesHandler : IRequestHandler<SetProductCategoriesCommand, Unit>
{
    private readonly DbContext _db;
    private readonly ICategoryReadService _cats;
    public SetProductCategoriesHandler(DbContext db, ICategoryReadService cats) { _db = db; _cats = cats; }

    public async Task<Unit> Handle(SetProductCategoriesCommand req, CancellationToken ct)
    {
        var p = await _db.Set<Product>()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == req.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        // desired categories are exactly what client selected
        var desired = req.CategoryIds.Distinct().ToHashSet();

        // validate only desired categories
        foreach (var cid in desired)
        {
            if (!await _cats.ExistsAsync(cid, ct))
                throw new InvalidOperationException($"Category {cid} not found.");
            if (!await _cats.IsLeafAsync(cid, ct))
                throw new InvalidOperationException($"Category {cid} must be a leaf.");
        }

        var current = p.Categories.Select(c => c.CategoryId).ToHashSet();
        var toRemove = current.Except(desired).ToList();
        var toAdd = desired.Except(current).ToList();

        // Determine final primary:
        // 1) If client provided and it's in desired => use it
        // 2) Else if current primary is still in desired => keep it
        // 3) Else if desired has any => pick the first
        // 4) Else no primary
        var requestedPrimary = req.PrimaryCategoryId.GetValueOrDefault();
        Guid newPrimary = Guid.Empty;
        if (requestedPrimary != Guid.Empty && desired.Contains(requestedPrimary))
            newPrimary = requestedPrimary;
        else if (p.PrimaryCategoryId.HasValue && desired.Contains(p.PrimaryCategoryId.Value))
            newPrimary = p.PrimaryCategoryId.Value;
        else if (desired.Count > 0)
            newPrimary = desired.First();

        // Use execution strategy to allow retryable explicit transaction
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Step 1: remove unselected links
                foreach (var cid in toRemove)
                    p.RemoveCategory(cid);

                // Step 2: clear primary first (to avoid filtered unique index conflicts)
                if (newPrimary != Guid.Empty)
                {
                    p.ClearPrimaryCategory();
                    await _db.SaveChangesAsync(ct);
                }

                // Step 3: add new links (non-primary)
                foreach (var cid in toAdd)
                    p.AddCategory(cid, makePrimary: false);

                await _db.SaveChangesAsync(ct);

                // Step 4: set primary if decided
                if (newPrimary != Guid.Empty)
                {
                    p.SetPrimaryCategory(newPrimary);
                    await _db.SaveChangesAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
        return Unit.Value;
    }
}
