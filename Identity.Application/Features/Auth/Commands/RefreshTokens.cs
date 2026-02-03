using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Security;
using Identity.Application.Models;
using Identity.Application.Options;

namespace Identity.Application.Features.Auth.Commands;

public sealed record RefreshTokensCommand(RefreshTokensRequest Request) : IRequest<AuthTokensDto>;

public sealed class RefreshTokensRequest
{
    public string RefreshToken { get; init; } = null!;
    public string? UserAgent { get; init; }
    public string? IpAddress { get; init; }
}

public sealed class RefreshTokensValidator : AbstractValidator<RefreshTokensCommand>
{
    public RefreshTokensValidator()
    {
        RuleFor(x => x.Request.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokensHandler : IRequestHandler<RefreshTokensCommand, AuthTokensDto>
{
    private readonly IIdentityDbContext _db;
    private readonly ITransactionRunner _tx;
    private readonly IJwtTokenService _jwt;
    private readonly IClock _clock;
    private readonly SessionOptions _sessionOptions;

    public RefreshTokensHandler(
        IIdentityDbContext db,
        ITransactionRunner tx,
        IJwtTokenService jwt,
        IClock clock,
        SessionOptions sessionOptions)
    {
        _db = db;
        _tx = tx;
        _jwt = jwt;
        _clock = clock;
        _sessionOptions = sessionOptions;
    }

    public async Task<AuthTokensDto> Handle(RefreshTokensCommand cmd, CancellationToken ct)
    {
        return await _tx.ExecuteAsync(async token =>
        {
            byte[] raw;
            try
            {
                raw = Crypto.Base64UrlDecode(cmd.Request.RefreshToken);
            }
            catch
            {
                throw new InvalidOperationException("رفرش توکن نامعتبر است.");
            }

            var hash = Crypto.Sha256(raw);
            var now = _clock.UtcNow;

            var session = await _db.UserSessions
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, token);

            if (session is null || !session.IsActive(now))
                throw new InvalidOperationException("رفرش توکن نامعتبر است یا منقضی شده است.");

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == session.UserId, token);

            if (user is null)
                throw new InvalidOperationException("کاربر یافت نشد.");

            if (user.IsCurrentlyBanned(now))
                throw new InvalidOperationException(AuthFailureMessages.BuildBanMessage(user));

            var newRaw = Crypto.RandomBytes(32);
            var newToken = Crypto.Base64UrlEncode(newRaw);
            var newHash = Crypto.Sha256(newRaw);
            var refreshExpiresAt = now.AddDays(_sessionOptions.RefreshTokenTtlDays);

            var newSession = Identity.Domain.Users.UserSession.Create(
                session.UserId,
                session.SiteId,
                newHash,
                refreshExpiresAt,
                cmd.Request.UserAgent ?? session.UserAgent,
                cmd.Request.IpAddress ?? session.IpAddress);

            session.Rotate(newHash, now, newSession.Id);
            _db.UserSessions.Add(newSession);
            await _db.SaveChangesAsync(token);

            var access = _jwt.CreateAccessToken(user.Id, session.SiteId, newSession.Id, user.PhoneNumber, now);
            return new AuthTokensDto(
                access,
                (int)_jwt.AccessTokenLifetime.TotalSeconds,
                newToken,
                user.Id,
                session.SiteId);
        }, ct);
    }
}
