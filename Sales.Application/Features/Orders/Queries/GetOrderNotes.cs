using MediatR;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Features.Orders.Models;

namespace Sales.Application.Features.Orders.Queries;

public sealed record GetOrderNotesQuery(
    Guid OrderId,
    bool IncludeInternal = true
) : IRequest<IReadOnlyList<OrderNoteDto>>;

public sealed class GetOrderNotesHandler : IRequestHandler<GetOrderNotesQuery, IReadOnlyList<OrderNoteDto>>
{
    private readonly ISalesDbContext _db;

    public GetOrderNotesHandler(ISalesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OrderNoteDto>> Handle(GetOrderNotesQuery req, CancellationToken ct)
    {
        var query = _db.OrderNotes
            .AsNoTracking()
            .Where(n => n.OrderId == req.OrderId);

        if (!req.IncludeInternal)
        {
            query = query.Where(n => !n.IsInternal);
        }

        var notes = await query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new OrderNoteDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                Note = n.Note,
                CreatedBy = n.CreatedBy,
                IsInternal = n.IsInternal,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            })
            .ToListAsync(ct);

        return notes;
    }
}
