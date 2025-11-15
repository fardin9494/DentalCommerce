using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Application.Products;

public sealed class SetProductDescriptionHandler : IRequestHandler<SetProductDescriptionCommand, Unit>
{
	private readonly DbContext _db;
	public SetProductDescriptionHandler(DbContext db) => _db = db;

	public async Task<Unit> Handle(SetProductDescriptionCommand req, CancellationToken ct)
	{
		var product = await _db.Set<Catalog.Domain.Products.Product>()
			.FirstOrDefaultAsync(p => p.Id == req.ProductId, ct);

		if (product is null)
			throw new InvalidOperationException("Product not found.");

		// اگر می‌خواهی Sanitization HTML اضافه کنیم، اینجا بهترین جاست.
		// فعلاً خام می‌گذاریم تا CKEditor هرچه فرستاد ذخیره شود.
		product.SetDescription(req.ContentHtml);

		await _db.SaveChangesAsync(ct);
		return Unit.Value;
	}
}