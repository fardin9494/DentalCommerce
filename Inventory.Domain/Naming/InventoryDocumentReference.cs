using System.Globalization;
using Inventory.Domain.Enums;

namespace Inventory.Domain.Naming;

internal static class InventoryDocumentReference
{
    internal static string ForReceipt(ReceiptReason reason, DateTime docDateUtc, long docNo)
        => $"RCPT-{ReceiptReasonCode(reason)}-{DateCode(docDateUtc)}-{NoToken(docNo)}";

    internal static string ForIssue(DateTime docDateUtc, long docNo)
        => $"ISS-{DateCode(docDateUtc)}-{NoToken(docNo)}";

    internal static string ForTransfer(DateTime docDateUtc, long docNo)
        => $"TRF-{DateCode(docDateUtc)}-{NoToken(docNo)}";

    internal static string ForAdjustment(AdjustmentReason reason, DateTime docDateUtc, long docNo)
        => $"ADJ-{AdjustmentReasonCode(reason)}-{DateCode(docDateUtc)}-{NoToken(docNo)}";

    private static string DateCode(DateTime docDateUtc)
        => DateTime.SpecifyKind(docDateUtc, DateTimeKind.Utc).ToString("yyMMdd", CultureInfo.InvariantCulture);

    private static string NoToken(long docNo)
        => docNo.ToString("D6", CultureInfo.InvariantCulture);

    private static string ReceiptReasonCode(ReceiptReason reason) =>
        reason switch
        {
            ReceiptReason.Purchase => "PUR",
            ReceiptReason.ReturnIn => "RETIN",
            ReceiptReason.Production => "PRD",
            _ => "OTH"
        };

    private static string AdjustmentReasonCode(AdjustmentReason reason) =>
        reason switch
        {
            AdjustmentReason.InitialBalance => "INIT",
            AdjustmentReason.Damage => "DMG",
            AdjustmentReason.Expired => "EXP",
            AdjustmentReason.Found => "FND",
            AdjustmentReason.Shrinkage => "SHR",
            AdjustmentReason.Correction => "COR",
            _ => "OTH"
        };
}
