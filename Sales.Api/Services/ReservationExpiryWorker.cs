using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;
using Sales.Infrastructure.Persistence;

namespace Sales.Api.Services;

public sealed class ReservationExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryWorker> _logger;
    private readonly TimeSpan _ttl;
    private readonly TimeSpan _interval;

    public ReservationExpiryWorker(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<ReservationExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var ttlMinutes = config.GetValue<int>("Sales:ReservationTtlMinutes", 15);
        var sweepMinutes = config.GetValue<int>("Sales:ReservationSweepMinutes", 1);

        _ttl = ttlMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(ttlMinutes);
        _interval = TimeSpan.FromMinutes(Math.Max(1, sweepMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_ttl <= TimeSpan.Zero)
        {
            _logger.LogInformation("Reservation expiry worker disabled (TTL <= 0).");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reservation expiry sweep failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();
        var inventory = scope.ServiceProvider.GetRequiredService<IInventoryReservationGateway>();

        var cutoff = DateTime.UtcNow - _ttl;
        var expired = await db.Orders
            .Include(o => o.Timeline)
            .Where(o => o.Status == OrderStatus.Draft && o.CreatedAt <= cutoff)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var order in expired)
        {
            try
            {
                await inventory.ReleaseAsync(order.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to release reservations for order {OrderId}", order.Id);
                continue;
            }

            order.MarkPaymentFailed("Payment timeout", $"Reservation expired after {_ttl.TotalMinutes} minutes at {DateTime.UtcNow:O}");
        }

        await db.SaveChangesAsync(ct);
    }
}
