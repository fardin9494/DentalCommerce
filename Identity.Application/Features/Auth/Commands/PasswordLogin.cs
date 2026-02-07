using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Admin;
using Identity.Application.Common.Audit;
using Identity.Application.Common.Security;
using Identity.Application.Models;
using Identity.Application.Options;
using Identity.Domain.Audit;
using Identity.Domain.Users;

namespace Identity.Application.Features.Auth.Commands;

public sealed record PasswordLoginCommand(PasswordLoginRequest Request) : IRequest<AuthTokensDto>;

public sealed class PasswordLoginRequest
{
    public string PhoneNumber { get; init; } = null!;
    public Guid SiteId { get; init; }
    public string Password { get; init; } = null!;
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
}

public sealed class PasswordLoginValidator : AbstractValidator<PasswordLoginCommand>
{
    public PasswordLoginValidator()
    {
        RuleFor(x => x.Request.PhoneNumber).NotEmpty();
        RuleFor(x => x.Request.SiteId).NotEmpty();
        RuleFor(x => x.Request.Password).NotEmpty();
    }
}

public sealed class PasswordLoginHandler : IRequestHandler<PasswordLoginCommand, AuthTokensDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private readonly SessionOptions _sessionOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly AdminOptions _adminOptions;

    public PasswordLoginHandler(
        IIdentityDbContext db,
        IJwtTokenService jwt,
        IClock clock,
        SessionOptions sessionOptions,
        SecurityOptions securityOptions,
        AdminOptions adminOptions)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
        _sessionOptions = sessionOptions;
        _securityOptions = securityOptions;
        _adminOptions = adminOptions;
    }

    public async Task<AuthTokensDto> Handle(PasswordLoginCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(cmd.Request.PhoneNumber);
        var now = _clock.UtcNow;

        await LoginGuardHelper.EnsureNotLockedAsync(_db, phone, now, ct);

        var user = await _db.Users
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.PhoneNumber == phone, ct);

        if (user is null || !user.HasPassword)
        {
            await LoginGuardHelper.RegisterFailureAsync(_db, _securityOptions, phone, now, ct);
            throw new InvalidOperationException("نام کاربری یا رمز عبور نادرست است.");
        }

        if (user.IsBanned && user.BanUntilUtc.HasValue && user.BanUntilUtc.Value <= now)
            user.Unban();

        if (user.IsCurrentlyBanned(now))
            throw new InvalidOperationException(AuthFailureMessages.BuildBanMessage(user));

        if (!PasswordHasher.Verify(cmd.Request.Password, user.PasswordSalt!, user.PasswordHash!, user.PasswordIterations!.Value))
        {
            await LoginGuardHelper.RegisterFailureAsync(_db, _securityOptions, phone, now, ct);
            throw new InvalidOperationException("نام کاربری یا رمز عبور نادرست است.");
        }

        user.EnsureMembership(cmd.Request.SiteId);
        user.MarkPhoneVerified(now);
        user.MarkLogin(now);

        await SuperAdminHelper.EnsureSuperAdminAsync(_db, _adminOptions, user, now, ct);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.LoginPassword,
            "ورود با رمز عبور",
            "ورود موفق",
            "user",
            user.Id,
            cmd.Request.IpAddress,
            cmd.Request.UserAgent,
            now));

        var refreshTokenRaw = Crypto.RandomBytes(32);
        var refreshToken = Crypto.Base64UrlEncode(refreshTokenRaw);
        var refreshTokenHash = Crypto.Sha256(refreshTokenRaw);
        var refreshExpiresAt = now.AddDays(_sessionOptions.RefreshTokenTtlDays);

        var session = UserSession.Create(
            user.Id,
            cmd.Request.SiteId,
            refreshTokenHash,
            refreshExpiresAt,
            cmd.Request.UserAgent,
            cmd.Request.IpAddress);

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        await LoginGuardHelper.ResetAsync(_db, phone, ct);

        var access = _jwt.CreateAccessToken(user.Id, cmd.Request.SiteId, session.Id, user.PhoneNumber, now);
        return new AuthTokensDto(
            access,
            (int)_jwt.AccessTokenLifetime.TotalSeconds,
            refreshToken,
            user.Id,
            cmd.Request.SiteId);
    }
}
