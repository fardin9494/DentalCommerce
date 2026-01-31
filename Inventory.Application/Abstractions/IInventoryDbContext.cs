using Inventory.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Inventory.Application.Abstractions;

public interface IInventoryDbContext
{
    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockReservation> StockReservations { get; }
    DbSet<StockItemSerial> StockItemSerials { get; }
    DbSet<StockLedgerEntry> StockLedger { get; }
    DbSet<Receipt> Receipts { get; }
    DbSet<ReceiptLine> ReceiptLines { get; }
    DbSet<Issue> Issues { get; }
    DbSet<IssueLine> IssueLines { get; }
    DbSet<IssueAllocation> IssueAllocations { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<TransferLine> TransferLines { get; }
    DbSet<TransferSegment> TransferSegments { get; }
    DbSet<StockShelf> StockShelves { get; }
    DbSet<InventoryCost> InventoryCosts { get; }
    DbSet<Adjustment> Adjustments { get; }
    DbSet<AdjustmentLine> AdjustmentLines { get; }

    Task<long> NextReceiptDocNoAsync(CancellationToken ct);
    Task<long> NextIssueDocNoAsync(CancellationToken ct);
    Task<long> NextTransferDocNoAsync(CancellationToken ct);
    Task<long> NextAdjustmentDocNoAsync(CancellationToken ct);

    ChangeTracker ChangeTracker { get; }
    EntityEntry Entry(object entity);
    Task<int> SaveChangesAsync(CancellationToken ct);
}
