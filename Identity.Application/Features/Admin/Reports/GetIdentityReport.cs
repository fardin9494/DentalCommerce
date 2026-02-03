using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Admin.Reports;

public sealed record GetIdentityReportQuery(int Days) : IRequest<IdentityReportDto>;

public sealed class GetIdentityReportHandler : IRequestHandler<GetIdentityReportQuery, IdentityReportDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public GetIdentityReportHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IdentityReportDto> Handle(GetIdentityReportQuery req, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var days = Math.Clamp(req.Days, 7, 90);
        var start = now.Date.AddDays(-(days - 1));

        var baseQuery =
            from u in _db.Users
            join g in _db.LoginGuards on u.PhoneNumber equals g.PhoneNumber into guards
            from g in guards.DefaultIfEmpty()
            select new { u, g };

        var totalUsers = await _db.Users.CountAsync(ct);

        var bannedUsers = await baseQuery.CountAsync(x =>
            x.u.IsBanned && (!x.u.BanUntilUtc.HasValue || x.u.BanUntilUtc > now), ct);

        var lockedUsers = await baseQuery.CountAsync(x =>
            x.g != null && x.g.LockedUntilUtc.HasValue && x.g.LockedUntilUtc > now, ct);

        var activeUsers = await baseQuery.CountAsync(x =>
            (!x.u.IsBanned || (x.u.BanUntilUtc.HasValue && x.u.BanUntilUtc <= now)) &&
            (x.g == null || !x.g.LockedUntilUtc.HasValue || x.g.LockedUntilUtc <= now), ct);

        var grouped = await _db.Users.AsNoTracking()
            .Where(x => x.CreatedAt >= start)
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new DailySignupDto(g.Key, g.Count()))
            .ToListAsync(ct);

        var map = grouped.ToDictionary(x => x.DateUtc.Date, x => x.Count);
        var daily = new List<DailySignupDto>(days);
        for (var i = 0; i < days; i++)
        {
            var day = start.AddDays(i);
            daily.Add(new DailySignupDto(day, map.TryGetValue(day, out var c) ? c : 0));
        }

        return new IdentityReportDto(totalUsers, activeUsers, bannedUsers, lockedUsers, daily);
    }
}
