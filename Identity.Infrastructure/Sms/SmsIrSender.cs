using Identity.Application.Abstractions;
using IPE.SmsIrClient;
using IPE.SmsIrClient.Exceptions;
using IPE.SmsIrClient.Models.Requests;

namespace Identity.Infrastructure.Sms;

public sealed class SmsIrSender : ISmsSender
{
    private readonly SmsIrOptions _options;
    private readonly SmsIr _client;

    public SmsIrSender(SmsIrOptions options)
    {
        _options = options;
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("SmsIr:ApiKey is required.");

        _client = new SmsIr(_options.ApiKey);
    }

    public async Task SendOtpAsync(string normalizedPhoneNumber, string code, CancellationToken ct)
    {
        try
        {
            if (_options.OtpTemplateId.HasValue)
            {
                var parameters = new[]
                {
                    new VerifySendParameter(_options.OtpParameterName, code)
                };

                var result = await _client.VerifySendAsync(
                    normalizedPhoneNumber,
                    _options.OtpTemplateId.Value,
                    parameters);

                if (result is null || result.Status != 1)
                    throw new InvalidOperationException(result?.Message ?? "SMS send failed.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_options.LineNumber))
                    throw new InvalidOperationException("SmsIr:LineNumber is required when SmsIr:OtpTemplateId is not configured.");

                if (!long.TryParse(_options.LineNumber, out var lineNumber))
                    throw new InvalidOperationException("SmsIr:LineNumber must be numeric.");

                var message = $"کد ورود: {code}";
                var result = await _client.BulkSendAsync(
                    lineNumber,
                    message,
                    new[] { normalizedPhoneNumber },
                    null);

                if (result is null || result.Status != 1)
                    throw new InvalidOperationException(result?.Message ?? "SMS send failed.");
            }
        }
        catch (SmsIrException ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }
}
