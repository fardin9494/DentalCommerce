using MediatR;

namespace Catalog.Application.Categories;

public sealed record RenameCategoryCommand(Guid CategoryId, string Name, string Slug) : IRequest<Unit>;
