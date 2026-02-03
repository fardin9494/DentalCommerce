using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Models;

namespace Identity.Application.Features.Users.Queries;

public sealed record GetMeQuery(Guid UserId) : IRequest<MeDto>;

public sealed class GetMeHandler : IRequestHandler<GetMeQuery, MeDto>
{
    private readonly IIdentityDbContext _db;

    public GetMeHandler(IIdentityDbContext db) => _db = db;

    public async Task<MeDto> Handle(GetMeQuery req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty) throw new ArgumentException("UserId required.", nameof(req.UserId));

        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == req.UserId, ct);

        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        return new MeDto(
            user.Id,
            user.PhoneNumber,
            user.FullName,
            user.NationalId,
            user.Address,
            user.PostalCode,
            user.Landline,
            user.BirthDateUtc,
            user.HasPassword,
            user.PhoneVerifiedAtUtc,
            user.CreatedAt);
    }
}
