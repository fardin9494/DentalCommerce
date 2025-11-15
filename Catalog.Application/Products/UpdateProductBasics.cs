using Catalog.Application.Categories;
using Catalog.Domain.Brands;
using Catalog.Domain.Products;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed record UpdateProductBasicsCommand(
    Guid ProductId,
    string Name,
    string Slug,
    string Code,
    Guid BrandId,
    string? WarehouseCode,
    string? CountryCode
) : IRequest<Unit>;

public sealed class UpdateProductBasicsValidator : AbstractValidator<UpdateProductBasicsCommand>
{
    public UpdateProductBasicsValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.WarehouseCode).MaximumLength(128).When(x => x.WarehouseCode != null);
        RuleFor(x => x.CountryCode).Length(2).When(x => !string.IsNullOrWhiteSpace(x.CountryCode));
    }
}

public sealed class UpdateProductBasicsHandler : IRequestHandler<UpdateProductBasicsCommand, Unit>
{
    private readonly DbContext _db;
    public UpdateProductBasicsHandler(DbContext db) => _db = db;

    public async Task<Unit> Handle(UpdateProductBasicsCommand req, CancellationToken ct)
    {
        var p = await _db.Set<Product>().FirstOrDefaultAsync(x => x.Id == req.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var brandExists = await _db.Set<Brand>().AnyAsync(b => b.Id == req.BrandId, ct);
        if (!brandExists) throw new InvalidOperationException("Brand not found.");

        var code = req.Code.Trim().ToUpperInvariant();
        var dup = await _db.Set<Product>().AnyAsync(x => x.Id != req.ProductId && x.Code == code, ct);
        if (dup) throw new InvalidOperationException("Duplicate product code.");

        p.Rename(req.Name);
        p.SetWarehouseCode(req.WarehouseCode);
        // Validate and set country if provided
        if (!string.IsNullOrWhiteSpace(req.CountryCode))
        {
            var ccode = req.CountryCode!.Trim().ToUpperInvariant();
            var exists = await _db.Set<Catalog.Domain.Brands.Country>().AnyAsync(c => c.Code2 == ccode, ct);
            if (!exists) throw new InvalidOperationException("Country not found.");
            p.SetCountry(ccode);
        }
        else
        {
            p.SetCountry(null);
        }

        var entry = _db.Entry(p);
        entry.Property(nameof(Product.DefaultSlug)).CurrentValue = req.Slug.Trim().ToLowerInvariant();
        entry.Property(nameof(Product.Code)).CurrentValue = code;
        entry.Property(nameof(Product.BrandId)).CurrentValue = req.BrandId;
        entry.Property(nameof(Product.CountryCode)).CurrentValue = string.IsNullOrWhiteSpace(req.CountryCode) ? null : req.CountryCode!.Trim().ToUpperInvariant();

        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
