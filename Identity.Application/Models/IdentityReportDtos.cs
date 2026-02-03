namespace Identity.Application.Models;

public sealed record DailySignupDto(DateTime DateUtc, int Count);

public sealed record IdentityReportDto(
    int TotalUsers,
    int ActiveUsers,
    int BannedUsers,
    int LockedUsers,
    IReadOnlyList<DailySignupDto> DailySignups);
