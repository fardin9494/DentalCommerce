using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Identity.Application.Abstractions;
using Identity.Application.Common.Audit;
using Identity.Application.Models;
using Identity.Domain.Audit;

namespace Identity.Application.Features.Users.Commands;

public sealed record UpdateMeCommand(Guid UserId, UpdateMeRequest Request) : IRequest<MeDto>;

public sealed class UpdateMeRequest
{
    public string? FullName { get; init; }
    public string? NationalId { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? Landline { get; init; }
    public DateTime? BirthDateUtc { get; init; }
}

public sealed class UpdateMeValidator : AbstractValidator<UpdateMeCommand>
{
    public UpdateMeValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Request.FullName).MaximumLength(200);
        RuleFor(x => x.Request.NationalId).MaximumLength(20);
        RuleFor(x => x.Request.Address).MaximumLength(1000);
        RuleFor(x => x.Request.PostalCode).MaximumLength(32);
        RuleFor(x => x.Request.Landline).MaximumLength(32);
    }
}

public sealed class UpdateMeHandler : IRequestHandler<UpdateMeCommand, MeDto>
{
    private readonly IIdentityDbContext _db;

    public UpdateMeHandler(IIdentityDbContext db) => _db = db;

    public async Task<MeDto> Handle(UpdateMeCommand cmd, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == cmd.UserId, ct);
        if (user is null) throw new InvalidOperationException("کاربر یافت نشد.");

        user.UpdateProfile(
            cmd.Request.FullName,
            cmd.Request.NationalId,
            cmd.Request.Address,
            cmd.Request.PostalCode,
            cmd.Request.Landline,
            cmd.Request.BirthDateUtc);

        _db.UserAuditEvents.Add(UserAuditEvent.Create(
            user.Id,
            AuditEventTypes.ProfileUpdated,
            "ویرایش پروفایل",
            "اطلاعات پروفایل به‌روزرسانی شد.",
            "user",
            user.Id,
            null,
            null,
            DateTime.UtcNow));

        await _db.SaveChangesAsync(ct);

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
