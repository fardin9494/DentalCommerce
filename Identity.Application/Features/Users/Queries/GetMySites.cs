using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Users.Queries;

public sealed record GetMySitesQuery(Guid UserId) : IRequest<IReadOnlyList<SiteMembershipDto>>;

public sealed class GetMySitesHandler : IRequestHandler<GetMySitesQuery, IReadOnlyList<SiteMembershipDto>>
{
    private readonly IIdentityDbContext _db;

    public GetMySitesHandler(IIdentityDbContext db) => _db = db;

    public async Task<IReadOnlyList<SiteMembershipDto>> Handle(GetMySitesQuery req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(req.UserId));

        return await _db.UserSiteMemberships
            .AsNoTracking()
            .Where(x => x.UserId == req.UserId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new SiteMembershipDto(x.SiteId, x.CreatedAt))
            .ToListAsync(ct);
    }
}

