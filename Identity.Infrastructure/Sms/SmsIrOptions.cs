namespace Identity.Infrastructure.Sms;

public sealed class SmsIrOptions
{
    public string ApiKey { get; init; } = null!;

    public string? LineNumber { get; init; }

    public int? OtpTemplateId { get; init; }
    public string OtpParameterName { get; init; } = "CODE";
}
