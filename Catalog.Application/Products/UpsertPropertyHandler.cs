using Catalog.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class UpsertPropertyHandler : IRequestHandler<UpsertPropertyCommand, Guid>
{
    private readonly DbContext _db;
    public UpsertPropertyHandler(DbContext db) => _db = db;

    public async Task<Guid> Handle(UpsertPropertyCommand req, CancellationToken ct)
    {
        var p = await _db.Set<Product>()
            .Include(x => x.Properties)
            .FirstOrDefaultAsync(x => x.Id == req.ProductId, ct);

        if (p is null) throw new InvalidOperationException("Product not found.");

        // آیا قبلاً این کلید برای محصول وجود دارد؟
        var prop = p.Properties.FirstOrDefault(x => x.Key == req.Key.Trim());

        if (prop is null)
        {
            // ایجاد صریح و افزودن صریح به EF
            prop = ProductProperty.Create(
                productId: p.Id,
                key: req.Key,
                s: req.ValueString,
                d: req.ValueDecimal,
                b: req.ValueBool,
                j: req.ValueJson
            );

            _db.Set<ProductProperty>().Add(prop);   // 👈 حتماً این را انجام بده
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // به‌روزرسانیِ مقدارها
            prop.Set(req.ValueString, req.ValueDecimal, req.ValueBool, req.ValueJson);
            await _db.SaveChangesAsync(ct);
        }

        return prop.Id;
    }
}