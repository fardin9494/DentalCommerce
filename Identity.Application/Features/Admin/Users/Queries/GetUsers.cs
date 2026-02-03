using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Admin.Users.Queries;

public sealed record GetUsersQuery(
    string? Query,
    string? Status,
    int Page = 1,
    int PageSize = 50) : IRequest<AdminUsersPageDto>;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, AdminUsersPageDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public GetUsersHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminUsersPageDto> Handle(GetUsersQuery req, CancellationToken ct)
    {
        var page = req.Page <= 0 ? 1 : req.Page;
        var pageSize = req.PageSize <= 0 ? 50 : Math.Min(200, req.PageSize);
        var now = _clock.UtcNow;
        var q = req.Query?.Trim();
        var status = (req.Status ?? string.Empty).Trim().ToLowerInvariant();

        var query =
            from u in _db.Users.AsNoTracking()
            join g in _db.LoginGuards.AsNoTracking() on u.PhoneNumber equals g.PhoneNumber into guards
            from guard in guards.DefaultIfEmpty()
            select new { u, guard };

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x =>
                x.u.PhoneNumber.Contains(q!) ||
                (x.u.FullName != null && x.u.FullName.Contains(q!)));
        }

        if (status == "banned")
        {
            query = query.Where(x =>
                x.u.IsBanned &&
                (!x.u.BanUntilUtc.HasValue || x.u.BanUntilUtc > now));
        }
        else if (status == "locked")
        {
            query = query.Where(x =>
                x.guard != null &&
                x.guard.LockedUntilUtc.HasValue &&
                x.guard.LockedUntilUtc > now);
        }
        else if (status == "active")
        {
            query = query.Where(x =>
                (!x.u.IsBanned || (x.u.BanUntilUtc.HasValue && x.u.BanUntilUtc <= now)) &&
                (x.guard == null || !x.guard.LockedUntilUtc.HasValue || x.guard.LockedUntilUtc <= now));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminUserSummaryDto(
                x.u.Id,
                x.u.PhoneNumber,
                x.u.FullName,
                x.u.CreatedAt,
                x.u.LastLoginAtUtc,
                x.u.IsBanned && (!x.u.BanUntilUtc.HasValue || x.u.BanUntilUtc > now),
                x.u.BanUntilUtc,
                x.u.BanReason,
                x.guard != null ? x.guard.LockedUntilUtc : null,
                x.guard != null ? x.guard.FailedCount : 0,
                x.u.HasPassword))
            .ToListAsync(ct);

        return new AdminUsersPageDto(total, items);
    }
}
