export const SalesPermissionKeys = {
  OrdersView: 'Sales.Orders.View',
  OrdersDetailView: 'Sales.Orders.Detail.View',
  OrdersTimelineView: 'Sales.Orders.Timeline.View',
  OrdersCreate: 'Sales.Orders.Create',
  OrdersShip: 'Sales.Orders.Ship',
  OrdersDeliver: 'Sales.Orders.Deliver',
  OrdersReturn: 'Sales.Orders.Return',
  OrdersRefund: 'Sales.Orders.Refund',
  OrdersRetryPayment: 'Sales.Orders.RetryPayment',
  OrdersCancel: 'Sales.Orders.Cancel',
  OrdersCancelLines: 'Sales.Orders.CancelLines',
  OrdersNotesView: 'Sales.Orders.Notes.View',
  OrdersNotesAdd: 'Sales.Orders.Notes.Add',

  RefundsView: 'Sales.Refunds.View',
  RefundsRequest: 'Sales.Refunds.Request',
  RefundsApprove: 'Sales.Refunds.Approve',
  RefundsReject: 'Sales.Refunds.Reject',
  RefundsComplete: 'Sales.Refunds.Complete',

  ReportsDailyView: 'Sales.Reports.Daily.View',
  ReportsMonthlyView: 'Sales.Reports.Monthly.View',
  ReportsBySiteView: 'Sales.Reports.BySite.View',
  ReportsByProductView: 'Sales.Reports.ByProduct.View',
  ReportsByCustomerView: 'Sales.Reports.ByCustomer.View',
} as const

export type SalesPermissionKey = typeof SalesPermissionKeys[keyof typeof SalesPermissionKeys]
