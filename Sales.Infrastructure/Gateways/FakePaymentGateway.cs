using Sales.Application.Abstractions;

namespace Sales.Infrastructure.Gateways;

public sealed class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        var success = request.Scenario == PaymentScenario.Success;
        var result = success
            ? new PaymentResult(true, null, $"FAKE-{Guid.NewGuid():N}")
            : new PaymentResult(false, "Payment rejected by fake gateway.");
        return Task.FromResult(result);
    }
}

