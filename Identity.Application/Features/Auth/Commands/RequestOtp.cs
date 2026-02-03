using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Security;
using Identity.Application.Options;
using Identity.Domain.Auth;
using Identity.Domain.Users;

namespace Identity.Application.Features.Auth.Commands;

public sealed record RequestOtpCommand(RequestOtpRequest Request) : IRequest<RequestOtpResult>;

public sealed record RequestOtpResult(DateTime ExpiresAtUtc, int TtlSeconds);

public sealed class RequestOtpRequest
{
    public string PhoneNumber { get; init; } = null!;
    public Guid SiteId { get; init; }
}

public sealed class RequestOtpValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpValidator()
    {
        RuleFor(x => x.Request.PhoneNumber).NotEmpty();
        RuleFor(x => x.Request.SiteId).NotEmpty();
    }
}

public sealed class RequestOtpHandler : IRequestHandler<RequestOtpCommand, RequestOtpResult>
{
    private readonly IIdentityDbContext _db;
    private readonly ISmsSender _sms;
    private readonly IClock _clock;
    private readonly OtpOptions _otpOptions;

    public RequestOtpHandler(
        IIdentityDbContext db,
        ISmsSender sms,
        IClock clock,
        OtpOptions otpOptions)
    {
        _db = db;
        _sms = sms;
        _clock = clock;
        _otpOptions = otpOptions;
    }

    public async Task<RequestOtpResult> Handle(RequestOtpCommand cmd, CancellationToken ct)
    {
        var phone = PhoneNumber.Normalize(cmd.Request.PhoneNumber);
        var now = _clock.UtcNow;

        await LoginGuardHelper.EnsureNotLockedAsync(_db, phone, now, ct);

        var existingUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PhoneNumber == phone, ct);

        if (existingUser is not null && existingUser.IsCurrentlyBanned(now))
            throw new InvalidOperationException(AuthFailureMessages.BuildBanMessage(existingUser));

        var latest = await _db.OtpChallenges
            .AsNoTracking()
            .Where(x => x.PhoneNumber == phone && x.SiteId == cmd.Request.SiteId && x.Purpose == OtpPurpose.Login)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (latest is not null)
        {
            var since = now - latest.CreatedAt;
            if (since.TotalSeconds < _otpOptions.SendCooldownSeconds)
                throw new InvalidOperationException("کد قبلاً ارسال شده است. لطفاً کمی بعد دوباره تلاش کنید.");
        }

        var code = Crypto.GenerateNumericCode(_otpOptions.CodeLength);
        var salt = Crypto.RandomBytes(16);
        var hash = Crypto.ComputeSaltedCodeHash(code, salt);
        var expiresAt = now.AddSeconds(_otpOptions.TtlSeconds);

        var challenge = OtpChallenge.Create(phone, cmd.Request.SiteId, OtpPurpose.Login, hash, salt, expiresAt);
        _db.OtpChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);

        await _sms.SendOtpAsync(phone, code, ct);

        return new RequestOtpResult(expiresAt, _otpOptions.TtlSeconds);
    }
}
