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
    public DbSet<OrderRefund> OrderRefunds => Set<OrderRefund>();
    public DbSet<OrderRefundLine> OrderRefundLines => Set<OrderRefundLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        ConfigureRowVersion(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureRowVersion(ModelBuilder modelBuilder)
    {
        const string rowVersionPropertyName = nameof(AggregateRoot<Guid>.RowVersion);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null) continue;

            var isAggregateRoot = IsAggregateRoot(entityType.ClrType);
            var prop = entityType.FindProperty(rowVersionPropertyName);
            
            if (prop is not null)
            {
                // Configure RowVersion for AggregateRoot entities
                prop.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate;
                prop.SetColumnType("rowversion");
                prop.IsConcurrencyToken = isAggregateRoot;
            }
        }
    }

    private static bool IsAggregateRoot(Type? type)
    {
        while (type is not null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>))
                return true;

            type = type.BaseType;
        }

        return false;
    }
}
