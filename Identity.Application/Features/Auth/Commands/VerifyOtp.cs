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

public sealed record VerifyOtpCommand(VerifyOtpRequest Request) : IRequest<AuthTokensDto>;

public sealed class VerifyOtpRequest
{
    public string PhoneNumber { get; init; } = null!;
    public Guid SiteId { get; init; }
    public string Code { get; init; } = null!;

    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
}

public sealed class VerifyOtpValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Request.PhoneNumber).NotEmpty();
        RuleFor(x => x.Request.SiteId).NotEmpty();
        RuleFor(x => x.Request.Code).NotEmpty().Length(4, 10);
    }
}

public sealed class VerifyOtpHandler : IRequestHandler<VerifyOtpCommand, AuthTokensDto>
{
    private readonly IIdentityDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private readonly OtpOptions _otpOptions;
    private readonly SessionOptions _sessionOptions;
    private readonly SecurityOptions _securityOptions;
    private readonly AdminOptions _adminOptions;

    public VerifyOtpHandler(
        IIdentityDbContext db,
        IJwtTokenService jwt,
        IClock clock,
        OtpOptions otpOptions,
        SessionOptions sessionOptions,
        SecurityOptions securityOptions,
        AdminOptions adminOptions)
    {
        _db = db;
        _jwt = jwt;
        _clock = clock;
        _otpOptions = otpOptions;
        _sessionOptions = sessionOptions;
        _securityOptions = securityOptions;
        _adminOptions = adminOptions;
    }

    public async Task<AuthTokensDto> Handle(VerifyOtpCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(cmd.Request.PhoneNumber);
        var now = _clock.UtcNow;

        await LoginGuardHelper.EnsureNotLockedAsync(_db, phone, now, ct);

        var challenge = await _db.OtpChallenges
            .Where(x =>
                x.PhoneNumber == phone &&
                x.SiteId == cmd.Request.SiteId &&
                x.VerifiedAtUtc == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (challenge is null || challenge.IsExpired(now))
        {
            await LoginGuardHelper.RegisterFailureAsync(_db, _securityOptions, phone, now, ct);
            throw new InvalidOperationException("کد تأیید نامعتبر است یا منقضی شده است.");
        }

        if (challenge.AttemptCount >= _otpOptions.MaxAttempts)
        {
            await LoginGuardHelper.RegisterFailureAsync(_db, _securityOptions, phone, now, ct);
            throw new InvalidOperationException("تعداد تلاش‌های ناموفق بیش از حد مجاز است. کمی بعد دوباره تلاش کنید.");
        }

        var computed = Crypto.ComputeSaltedCodeHash(cmd.Request.Code, challenge.Salt);
        if (!Crypto.FixedTimeEquals(computed, challenge.CodeHash))
        {
            challenge.MarkFailedAttempt();
            await _db.SaveChangesAsync(ct);
            await LoginGuardHelper.RegisterFailureAsync(_db, _securityOptions, phone, now, ct);
            throw new InvalidOperationException("کد تأیید اشتباه است.");
        }

        challenge.MarkVerified(now);

        var user = await _db.Users
            .Include(x => x.Memberships)
            .FirstOrDefaultAsync(x => x.PhoneNumber == phone, ct);

        if (user is null)
        {
            user = User.Create(phone);
            user.MarkPhoneVerified(now);
            user.EnsureMembership(cmd.Request.SiteId);
            _db.Users.Add(user);
        }
        else
        {
            if (user.IsBanned && user.BanUntilUtc.HasValue && user.BanUntilUtc.Value <= now)
                user.Unban();

            if (user.IsCurrentlyBanned(now))
                throw new InvalidOperationException(AuthFailureMessages.BuildBanMessage(user));

            user.MarkPhoneVerified(now);
            user.EnsureMembership(cmd.Request.SiteId);
        }

        user.MarkLogin(now);

        await SuperAdminHelper.EnsureSuperAdminAsync(_db, _adminOptions, user, now, ct);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.LoginOtp,
            "ورود با کد پیامکی",
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
