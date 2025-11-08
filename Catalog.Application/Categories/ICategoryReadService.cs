namespace Catalog.Application.Categories;

public interface ICategoryReadService
{
    Task<bool> ExistsAsync(Guid categoryId, CancellationToken ct);
    Task<bool> IsLeafAsync(Guid categoryId, CancellationToken ct);
}