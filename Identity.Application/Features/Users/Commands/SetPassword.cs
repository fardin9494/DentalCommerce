using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Application.Common.Security;
using Identity.Application.Options;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Users.Commands;

public sealed record SetPasswordCommand(Guid UserId, SetPasswordRequest Request) : IRequest;

public sealed class SetPasswordRequest
{
    public string Password { get; init; } = null!;
}

public sealed class SetPasswordValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordValidator(PasswordOptions options)
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.Password).NotEmpty().MinimumLength(options.MinLength);
    }
}

public sealed class SetPasswordHandler : IRequestHandler<SetPasswordCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;
    private readonly PasswordOptions _options;

    public SetPasswordHandler(IIdentityDbContext db, IClock clock, PasswordOptions options)
    {
        _db = db;
        _clock = clock;
        _options = options;
    }

    public async Task Handle(SetPasswordCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        var result = PasswordHasher.HashPassword(cmd.Request.Password, _options.Pbkdf2Iterations);
        user.SetPassword(result.Hash, result.Salt, result.Iterations, result.Algorithm, _clock.UtcNow);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.PasswordSet,
            "ثبت/تغییر رمز عبور",
            "رمز عبور با موفقیت ثبت شد.",
            "user",
            user.Id,
            null,
            null,
            _clock.UtcNow));

        await _db.SaveChangesAsync(ct);
    }
}
