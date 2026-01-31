using System.Reflection;
using BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence;

public sealed class SalesDbContext : DbContext, ISalesDbContext
{
    public const string DefaultSchema = "sales";

    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<OrderTimelineEntry> OrderTimeline => Set<OrderTimelineEntry>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        IgnoreRowVersion(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void IgnoreRowVersion(ModelBuilder modelBuilder)
    {
        const string rowVersionPropertyName = nameof(AggregateRoot<Guid>.RowVersion);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var prop = entityType.FindProperty(rowVersionPropertyName) ?? entityType.FindProperty("RowVersion");
            if (prop is null) continue;
            modelBuilder.Entity(entityType.ClrType).Ignore(prop.Name);
        }
    }
}
