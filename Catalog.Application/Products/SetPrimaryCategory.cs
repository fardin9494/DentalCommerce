using Catalog.Application.Categories;
using Catalog.Domain.Products;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed record SetPrimaryCategoryCommand(Guid ProductId, Guid CategoryId) : IRequest<Unit>;

public sealed class SetPrimaryCategoryValidator : AbstractValidator<SetPrimaryCategoryCommand>
{
    public SetPrimaryCategoryValidator(ICategoryReadService cats)
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.CategoryId).MustAsync(async (cid, ct) => await cats.ExistsAsync(cid, ct))
            .WithMessage("Category not found.");
        RuleFor(x => x.CategoryId).MustAsync(async (cid, ct) => await cats.IsLeafAsync(cid, ct))
            .WithMessage("Category must be a leaf.");
    }
}

public sealed class SetPrimaryCategoryHandler : IRequestHandler<SetPrimaryCategoryCommand, Unit>
{
    private readonly DbContext _db;
    public SetPrimaryCategoryHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(SetPrimaryCategoryCommand req, CancellationToken ct)
    {
        var p = await _db.Set<Product>()
            .Include(x => x.Categories)
            .FirstOrDefaultAsync(x => x.Id == req.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        // ensure link exists
        if (!p.Categories.Any(c => c.CategoryId == req.CategoryId))
            p.AddCategory(req.CategoryId, makePrimary: false);

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                p.ClearPrimaryCategory();
                await _db.SaveChangesAsync(ct);

                p.SetPrimaryCategory(req.CategoryId);
                await _db.SaveChangesAsync(ct);

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

