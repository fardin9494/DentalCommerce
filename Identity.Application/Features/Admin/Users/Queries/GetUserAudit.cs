using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Admin.Users.Queries;

public sealed record GetUserAuditQuery(Guid UserId, int Page, int PageSize) : IRequest<UserAuditListDto>;

public sealed class GetUserAuditHandler : IRequestHandler<GetUserAuditQuery, UserAuditListDto>
{
    private readonly IIdentityDbContext _db;

    public GetUserAuditHandler(IIdentityDbContext db) => _db = db;

    public async Task<UserAuditListDto> Handle(GetUserAuditQuery req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(req.UserId));
        var page = Math.Max(req.Page, 1);
        var pageSize = Math.Clamp(req.PageSize, 1, 200);

        var query = _db.UserAuditEvents.AsNoTracking().Where(x => x.UserId == req.UserId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserAuditItemDto(
                x.Id,
                x.EventType,
                x.Title,
                x.Detail,
                x.ActorType,
                x.ActorUserId,
                x.IpAddress,
                x.UserAgent,
                x.CreatedAt))
            .ToListAsync(ct);

        return new UserAuditListDto(total, items);
    }
}
