namespace Inventory.Api.Permissions;

public static class InventoryPermissionKeys
{
    public const string DashboardView = "Inventory.Dashboard.View";

    public const string ReceiptsView = "Inventory.Receipts.View";
    public const string ReceiptsCreate = "Inventory.Receipts.Create";
    public const string ReceiptsEdit = "Inventory.Receipts.Edit";
    public const string ReceiptsReceive = "Inventory.Receipts.Receive";
    public const string ReceiptsApprove = "Inventory.Receipts.Approve";
    public const string ReceiptsReject = "Inventory.Receipts.Reject";
    public const string ReceiptsCancel = "Inventory.Receipts.Cancel";

    public const string ReceiptRejectionsView = "Inventory.ReceiptRejections.View";
    public const string ReceiptRejectionsResolve = "Inventory.ReceiptRejections.Resolve";

    public const string IssuesView = "Inventory.Issues.View";
    public const string IssuesCreate = "Inventory.Issues.Create";
    public const string IssuesEdit = "Inventory.Issues.Edit";
    public const string IssuesAllocate = "Inventory.Issues.Allocate";
    public const string IssuesPost = "Inventory.Issues.Post";
    public const string IssuesCancel = "Inventory.Issues.Cancel";

    public const string TransfersView = "Inventory.Transfers.View";
    public const string TransfersCreate = "Inventory.Transfers.Create";
    public const string TransfersEdit = "Inventory.Transfers.Edit";
    public const string TransfersAllocate = "Inventory.Transfers.Allocate";
    public const string TransfersShip = "Inventory.Transfers.Ship";
    public const string TransfersReceive = "Inventory.Transfers.Receive";
    public const string TransfersComplete = "Inventory.Transfers.Complete";
    public const string TransfersCancel = "Inventory.Transfers.Cancel";

    public const string AdjustmentsView = "Inventory.Adjustments.View";
    public const string AdjustmentsCreate = "Inventory.Adjustments.Create";
    public const string AdjustmentsEdit = "Inventory.Adjustments.Edit";
    public const string AdjustmentsPost = "Inventory.Adjustments.Post";
    public const string AdjustmentsCancel = "Inventory.Adjustments.Cancel";

    public const string CostsView = "Inventory.Costs.View";
    public const string CostsEdit = "Inventory.Costs.Edit";

    public const string ShelvesView = "Inventory.Shelves.View";
    public const string ShelvesManage = "Inventory.Shelves.Manage";
    public const string OperationsMoveStock = "Inventory.Operations.MoveStock";

    public const string WarehouseView = "Inventory.Warehouse.View";
    public const string WarehouseCreate = "Inventory.Warehouse.Create";
    public const string WarehouseEdit = "Inventory.Warehouse.Edit";
    public const string WarehouseActivate = "Inventory.Warehouse.Activate";

    public const string StockLedgerView = "Inventory.StockLedger.View";
    public const string StockItemsView = "Inventory.StockItems.View";
    public const string StockItemsSerialsView = "Inventory.StockItems.Serials.View";
    public const string StockItemsUnassignedView = "Inventory.StockItems.Unassigned.View";
    public const string StockItemsProductsView = "Inventory.StockItems.Products.View";

    public const string ReservationsView = "Inventory.Reservations.View";
    public const string ReservationsManage = "Inventory.Reservations.Manage";

    public const string CatalogProxy = "Inventory.Catalog.Proxy";
    public const string ReportsView = "Inventory.Reports.View";
    public const string AuditView = "Inventory.Audit.View";
}
