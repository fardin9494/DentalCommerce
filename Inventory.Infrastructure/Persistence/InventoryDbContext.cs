using BuildingBlocks.Domain;
using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Reflection;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext
{
    public const string DefaultSchema = "inv";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockLedgerEntry> StockLedger => Set<StockLedgerEntry>();
    public DbSet<Receipt> Receipts => Set<Receipt>();
    public DbSet<ReceiptLine> ReceiptLines => Set<ReceiptLine>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<IssueLine> IssueLines => Set<IssueLine>();
    public DbSet<IssueAllocation> IssueAllocations => Set<IssueAllocation>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferLine> TransferLines => Set<TransferLine>();
    public DbSet<TransferSegment> TransferSegments => Set<TransferSegment>();
    public DbSet<StockShelf> StockShelves => Set<StockShelf>();
    public DbSet<InventoryCost> InventoryCosts => Set<InventoryCost>();
    public DbSet<Adjustment> Adjustments => Set<Adjustment>();
    public DbSet<AdjustmentLine> AdjustmentLines => Set<AdjustmentLine>();

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

            var prop = entityType.FindProperty(rowVersionPropertyName) ?? entityType.FindProperty("RowVersion");
            if (prop is null) continue;

                prop.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                prop.SetColumnType("rowversion");

            var isAggregateRoot = IsAggregateRoot(entityType.ClrType);
            prop.IsConcurrencyToken = isAggregateRoot;

            if (!isAggregateRoot)
            {
                prop.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
                prop.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
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