using System;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public static class InventoryDbContextSequences
{
    public static Task<long> NextReceiptDocNoAsync(this InventoryDbContext db, CancellationToken ct)
        => db.NextSequenceValueAsync("inv.ReceiptDocNoSeq", ct);

    public static Task<long> NextIssueDocNoAsync(this InventoryDbContext db, CancellationToken ct)
        => db.NextSequenceValueAsync("inv.IssueDocNoSeq", ct);

    public static Task<long> NextTransferDocNoAsync(this InventoryDbContext db, CancellationToken ct)
        => db.NextSequenceValueAsync("inv.TransferDocNoSeq", ct);

    public static Task<long> NextAdjustmentDocNoAsync(this InventoryDbContext db, CancellationToken ct)
        => db.NextSequenceValueAsync("inv.AdjustmentDocNoSeq", ct);

    private static async Task<long> NextSequenceValueAsync(this InventoryDbContext db, string sequenceName, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose) await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT NEXT VALUE FOR {sequenceName};";
            var result = await cmd.ExecuteScalarAsync(ct);
            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose) await db.Database.CloseConnectionAsync();
        }
    }
}
