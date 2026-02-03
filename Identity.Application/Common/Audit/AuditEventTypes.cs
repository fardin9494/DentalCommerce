namespace Identity.Application.Common.Audit;

public static class AuditEventTypes
{
    public const string LoginOtp = "login_otp";
    public const string LoginPassword = "login_password";
    public const string ProfileUpdated = "profile_updated";
    public const string PasswordSet = "password_set";
    public const string PasswordCleared = "password_cleared";
    public const string UserBanned = "user_banned";
    public const string UserUnbanned = "user_unbanned";
    public const string UserUnlocked = "user_unlocked";
    public const string SessionsRevoked = "sessions_revoked";
    public const string PasswordResetByAdmin = "password_reset_admin";
}
