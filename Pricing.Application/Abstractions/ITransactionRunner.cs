namespace Pricing.Application.Abstractions;

public interface ITransactionRunner
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct);
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);
}
