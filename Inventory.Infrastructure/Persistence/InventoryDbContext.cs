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

            var isAggregateRoot = IsAggregateRoot(entityType.ClrType);
            
            // ابتدا سعی می‌کنیم RowVersion را از AggregateRoot پیدا کنیم
            var prop = entityType.FindProperty(rowVersionPropertyName);
            
            // اگر پیدا نشد و entity یک child entity است که در دیتابیس RowVersion دارد،
            // باید آن را به صورت shadow property اضافه کنیم
            if (prop is null && !isAggregateRoot)
            {
                // بررسی می‌کنیم که آیا این entity در migration قبلی RowVersion داشته یا نه
                // لیست entities که در migration AddRowVersionToInventoryDocs RowVersion اضافه شده:
                var entitiesWithRowVersionInDb = new[]
                {
                    "ReceiptLine", "IssueLine", "IssueAllocation", "TransferLine", 
                    "TransferSegment", "AdjustmentLine", "InventoryCost"
                };
                
                var entityName = entityType.ClrType.Name;
                if (entitiesWithRowVersionInDb.Contains(entityName))
                {
                    // اضافه کردن RowVersion به صورت shadow property برای child entities
                    var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    entityBuilder.Property<byte[]>("RowVersion")
                        .IsRequired() // non-nullable برای هماهنگی با دیتابیس
                        .HasColumnType("rowversion")
                        .ValueGeneratedOnAddOrUpdate()
                        .IsConcurrencyToken(false); // برای child entities concurrency token نیست
                }
                
                continue;
            }
            
            // اگر RowVersion پیدا شد (از AggregateRoot)
            if (prop is not null)
            {
                // پیکربندی RowVersion
                prop.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                prop.SetColumnType("rowversion");
                
                // فقط برای AggregateRoots به عنوان ConcurrencyToken استفاده می‌شود
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