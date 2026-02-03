using Identity.Domain.Users;

namespace Identity.Application.Common.Security;

public static class AuthFailureMessages
{
    public static string BuildBanMessage(User user)
    {
        var reason = string.IsNullOrWhiteSpace(user.BanReason) ? "بدون دلیل ثبت‌شده" : user.BanReason!.Trim();
        var until = user.BanUntilUtc.HasValue ? user.BanUntilUtc.Value.ToLocalTime().ToString("yyyy/MM/dd HH:mm") : null;

        return until is null
            ? $"این حساب مسدود است. علت: {reason}"
            : $"این حساب تا {until} مسدود است. علت: {reason}";
    }
}
