using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Admin.Users.Commands;

public sealed record BanUserCommand(Guid UserId, BanUserRequest Request) : IRequest;

public sealed class BanUserRequest
{
    public string? Reason { get; init; }
    public DateTime? UntilUtc { get; init; }
}

public sealed class BanUserValidator : AbstractValidator<BanUserCommand>
{
    public BanUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.Reason).MaximumLength(512);
    }
}

public sealed class BanUserHandler : IRequestHandler<BanUserCommand>
{
    private readonly IIdentityDbContext _db;
    private readonly IClock _clock;

    public BanUserHandler(IIdentityDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task Handle(BanUserCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        var until = cmd.Request.UntilUtc;
        if (until.HasValue && until.Value.Kind != DateTimeKind.Utc)
            until = DateTime.SpecifyKind(until.Value, DateTimeKind.Utc);

        user.Ban(_clock.UtcNow, cmd.Request.Reason, until);

        var reason = string.IsNullOrWhiteSpace(cmd.Request.Reason) ? "بدون دلیل ثبت‌شده" : cmd.Request.Reason!.Trim();
        var untilText = until.HasValue ? until.Value.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : "نامحدود";
        var detail = $"علت: {reason} | تا: {untilText}";

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.UserBanned,
            "بن کاربر",
            detail,
            "admin",
            null,
            null,
            null,
            _clock.UtcNow));

        await _db.SaveChangesAsync(ct);
    }
}
