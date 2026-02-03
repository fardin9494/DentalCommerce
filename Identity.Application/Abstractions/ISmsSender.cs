namespace Identity.Application.Abstractions;

public interface ISmsSender
{
    Task SendOtpAsync(string normalizedPhoneNumber, string code, CancellationToken ct);
}
