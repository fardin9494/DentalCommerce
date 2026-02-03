using Identity.Application.Abstractions;

namespace Identity.Api.Services;

public sealed class DevSmsSender : ISmsSender
{
    private readonly ILogger<DevSmsSender> _logger;

    public DevSmsSender(ILogger<DevSmsSender> logger) => _logger = logger;

    public Task SendOtpAsync(string normalizedPhoneNumber, string code, CancellationToken ct)
    {
        _logger.LogWarning("DEV SMS (no-op) to {Phone}: {Code}", normalizedPhoneNumber, code);
        return Task.CompletedTask;
    }
}
