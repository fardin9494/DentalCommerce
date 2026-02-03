using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Application.Common.Security;
using Identity.Application.Options;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Admin.Users.Commands;

public sealed record ResetUserPasswordCommand(Guid UserId, ResetUserPasswordRequest Request) : IRequest;

public sealed class ResetUserPasswordRequest
{
    public string? NewPassword { get; init; }
    public bool Clear { get; init; }
}

public sealed class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.NewPassword).MaximumLength(200);
    }
}

public sealed class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;
    private readonly PasswordOptions _options;

    public ResetUserPasswordHandler(IIdentityDbContext db, IClock clock, PasswordOptions options)
    {
        _db = db;
        _clock = clock;
        _options = options;
    }

    public async Task Handle(ResetUserPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        var pwd = cmd.Request.NewPassword?.Trim();
        if (cmd.Request.Clear || string.IsNullOrWhiteSpace(pwd))
        {
            user.ClearPassword();
            _db.UserAuditEvents.Add(UserAuditEvent.Create(
                user.Id,
                AuditEventTypes.PasswordCleared,
                "حذف رمز عبور",
                "رمز عبور توسط ادمین حذف شد.",
                "admin",
                null,
                null,
                null,
                _clock.UtcNow));
        }
        else
        {
            if (pwd.Length < _options.MinLength)
                throw new InvalidOperationException($"حداقل طول رمز عبور باید {_options.MinLength} باشد.");

            var (hash, salt, iterations, algo) = PasswordHasher.HashPassword(pwd, _options.Pbkdf2Iterations);
            user.SetPassword(hash, salt, iterations, algo, _clock.UtcNow);
            _db.UserAuditEvents.Add(UserAuditEvent.Create(
                user.Id,
                AuditEventTypes.PasswordResetByAdmin,
                "بازنشانی رمز عبور",
                "رمز عبور توسط ادمین تغییر کرد.",
                "admin",
                null,
                null,
                null,
                _clock.UtcNow));
        }

        await _db.SaveChangesAsync(ct);
    }
}
