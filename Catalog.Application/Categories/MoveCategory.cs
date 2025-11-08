using MediatR;

namespace Catalog.Application.Categories;

public sealed record MoveCategoryCommand(Guid CategoryId, Guid? NewParentId) : IRequest<Unit>;