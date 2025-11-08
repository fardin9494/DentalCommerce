using MediatR;

namespace Catalog.Application.Categories;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId   // null = ریشه
) : IRequest<Guid>;