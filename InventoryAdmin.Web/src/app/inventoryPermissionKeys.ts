export const InventoryPermissionKeys = {
  DashboardView: 'Inventory.Dashboard.View',

  ReceiptsView: 'Inventory.Receipts.View',
  ReceiptsCreate: 'Inventory.Receipts.Create',
  ReceiptsEdit: 'Inventory.Receipts.Edit',
  ReceiptsReceive: 'Inventory.Receipts.Receive',
  ReceiptsApprove: 'Inventory.Receipts.Approve',
  ReceiptsReject: 'Inventory.Receipts.Reject',
  ReceiptsCancel: 'Inventory.Receipts.Cancel',

  ReceiptRejectionsView: 'Inventory.ReceiptRejections.View',
  ReceiptRejectionsResolve: 'Inventory.ReceiptRejections.Resolve',

  IssuesView: 'Inventory.Issues.View',
  IssuesCreate: 'Inventory.Issues.Create',
  IssuesEdit: 'Inventory.Issues.Edit',
  IssuesAllocate: 'Inventory.Issues.Allocate',
  IssuesPost: 'Inventory.Issues.Post',
  IssuesCancel: 'Inventory.Issues.Cancel',

  TransfersView: 'Inventory.Transfers.View',
  TransfersCreate: 'Inventory.Transfers.Create',
  TransfersEdit: 'Inventory.Transfers.Edit',
  TransfersAllocate: 'Inventory.Transfers.Allocate',
  TransfersShip: 'Inventory.Transfers.Ship',
  TransfersReceive: 'Inventory.Transfers.Receive',
  TransfersComplete: 'Inventory.Transfers.Complete',
  TransfersCancel: 'Inventory.Transfers.Cancel',

  AdjustmentsView: 'Inventory.Adjustments.View',
  AdjustmentsCreate: 'Inventory.Adjustments.Create',
  AdjustmentsEdit: 'Inventory.Adjustments.Edit',
  AdjustmentsPost: 'Inventory.Adjustments.Post',
  AdjustmentsCancel: 'Inventory.Adjustments.Cancel',

  CostsView: 'Inventory.Costs.View',
  CostsEdit: 'Inventory.Costs.Edit',

  ShelvesView: 'Inventory.Shelves.View',
  ShelvesManage: 'Inventory.Shelves.Manage',
  OperationsMoveStock: 'Inventory.Operations.MoveStock',

  WarehouseView: 'Inventory.Warehouse.View',
  WarehouseCreate: 'Inventory.Warehouse.Create',
  WarehouseEdit: 'Inventory.Warehouse.Edit',
  WarehouseActivate: 'Inventory.Warehouse.Activate',

  StockLedgerView: 'Inventory.StockLedger.View',
  StockItemsView: 'Inventory.StockItems.View',
  StockItemsSerialsView: 'Inventory.StockItems.Serials.View',
  StockItemsUnassignedView: 'Inventory.StockItems.Unassigned.View',
  StockItemsProductsView: 'Inventory.StockItems.Products.View',

  ReservationsView: 'Inventory.Reservations.View',
  ReservationsManage: 'Inventory.Reservations.Manage',

  CatalogProxy: 'Inventory.Catalog.Proxy',
  ReportsView: 'Inventory.Reports.View',
  AuditView: 'Inventory.Audit.View',
} as const

export type InventoryPermissionKey = typeof InventoryPermissionKeys[keyof typeof InventoryPermissionKeys]
