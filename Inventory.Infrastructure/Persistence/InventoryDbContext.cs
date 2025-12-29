using BuildingBlocks.Domain;
using Inventory.Application.Abstractions;
using Inventory.Domain.Aggregates;
using Inventory.Domain.Markers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Data;
using System.Reflection;

namespace Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext : DbContext, IInventoryDbContext
{
    public const string DefaultSchema = "inv";

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockItemSerial> StockItemSerials => Set<StockItemSerial>();
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
            
            // Ø§Ø¨ØªØ¯Ø§ Ø³Ø¹ÛŒ Ù…ÛŒâ€ŒÚ©Ù†ÛŒÙ… RowVersion Ø±Ø§ Ø§Ø² AggregateRoot Ù¾ÛŒØ¯Ø§ Ú©Ù†ÛŒÙ…
            var prop = entityType.FindProperty(rowVersionPropertyName);
            
            // Ø§Ú¯Ø± Ù¾ÛŒØ¯Ø§ Ù†Ø´Ø¯ Ùˆ entity ÛŒÚ© child entity Ø§Ø³Øª Ú©Ù‡ Ø¯Ø± Ø¯ÛŒØªØ§Ø¨ÛŒØ³ RowVersion Ø¯Ø§Ø±Ø¯ØŒ
            // Ø¨Ø§ÛŒØ¯ Ø¢Ù† Ø±Ø§ Ø¨Ù‡ ØµÙˆØ±Øª shadow property Ø§Ø¶Ø§ÙÙ‡ Ú©Ù†ÛŒÙ…
            if (prop is null && !isAggregateRoot)
            {
                if (typeof(IHasRowVersion).IsAssignableFrom(entityType.ClrType))
                {
                    var entityBuilder = modelBuilder.Entity(entityType.ClrType);
                    entityBuilder.Property<byte[]>("RowVersion")
                        .IsRequired()
                        .HasColumnType("rowversion")
                        .ValueGeneratedOnAddOrUpdate()
                        .IsConcurrencyToken(false);
                }

                continue;
            }
            
            // Ø§Ú¯Ø± RowVersion Ù¾ÛŒØ¯Ø§ Ø´Ø¯ (Ø§Ø² AggregateRoot)
            if (prop is not null)
            {
                // Ù¾ÛŒÚ©Ø±Ø¨Ù†Ø¯ÛŒ RowVersion
                prop.ValueGenerated = ValueGenerated.OnAddOrUpdate;
                prop.SetColumnType("rowversion");
                
                // ÙÙ‚Ø· Ø¨Ø±Ø§ÛŒ AggregateRoots Ø¨Ù‡ Ø¹Ù†ÙˆØ§Ù† ConcurrencyToken Ø§Ø³ØªÙØ§Ø¯Ù‡ Ù…ÛŒâ€ŒØ´ÙˆØ¯
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

    public Task<long> NextReceiptDocNoAsync(CancellationToken ct)
        => NextSequenceValueAsync("inv.ReceiptDocNoSeq", ct);

    public Task<long> NextIssueDocNoAsync(CancellationToken ct)
        => NextSequenceValueAsync("inv.IssueDocNoSeq", ct);

    public Task<long> NextTransferDocNoAsync(CancellationToken ct)
        => NextSequenceValueAsync("inv.TransferDocNoSeq", ct);

    public Task<long> NextAdjustmentDocNoAsync(CancellationToken ct)
        => NextSequenceValueAsync("inv.AdjustmentDocNoSeq", ct);

    private async Task<long> NextSequenceValueAsync(string sequenceName, CancellationToken ct)
    {
        var connection = Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose) await Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT NEXT VALUE FOR {sequenceName};";
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose) await Database.CloseConnectionAsync();
        }
    }
}


