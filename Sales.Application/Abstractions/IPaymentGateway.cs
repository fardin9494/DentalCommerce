namespace Sales.Application.Abstractions;

public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct);
}

public sealed record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    PaymentScenario Scenario);

public sealed record PaymentResult(
    bool Success,
    string? FailureReason = null,
    string? GatewayRef = null);

public enum PaymentScenario
{
    Success = 1,
    Fail = 2
}

