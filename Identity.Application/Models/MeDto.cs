namespace Identity.Application.Models;

public sealed record MeDto(
    Guid UserId,
    string PhoneNumber,
    string? FullName,
    string? NationalId,
    string? Address,
    string? PostalCode,
    string? Landline,
    DateTime? BirthDateUtc,
    bool HasPassword,
    DateTime? PhoneVerifiedAtUtc,
    DateTime CreatedAtUtc);
