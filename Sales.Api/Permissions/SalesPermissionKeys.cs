namespace Sales.Api.Permissions;

public static class SalesPermissionKeys
{
    public const string OrdersView = "Sales.Orders.View";
    public const string OrdersDetailView = "Sales.Orders.Detail.View";
    public const string OrdersTimelineView = "Sales.Orders.Timeline.View";
    public const string OrdersCreate = "Sales.Orders.Create";
    public const string OrdersShip = "Sales.Orders.Ship";
    public const string OrdersDeliver = "Sales.Orders.Deliver";
    public const string OrdersReturn = "Sales.Orders.Return";
    public const string OrdersRefund = "Sales.Orders.Refund";
    public const string OrdersRetryPayment = "Sales.Orders.RetryPayment";
    public const string OrdersCancel = "Sales.Orders.Cancel";
    public const string OrdersCancelLines = "Sales.Orders.CancelLines";
    public const string OrdersNotesView = "Sales.Orders.Notes.View";
    public const string OrdersNotesAdd = "Sales.Orders.Notes.Add";

    public const string RefundsView = "Sales.Refunds.View";
    public const string RefundsRequest = "Sales.Refunds.Request";
    public const string RefundsApprove = "Sales.Refunds.Approve";
    public const string RefundsReject = "Sales.Refunds.Reject";
    public const string RefundsComplete = "Sales.Refunds.Complete";

    public const string ReportsDailyView = "Sales.Reports.Daily.View";
    public const string ReportsMonthlyView = "Sales.Reports.Monthly.View";
    public const string ReportsBySiteView = "Sales.Reports.BySite.View";
    public const string ReportsByProductView = "Sales.Reports.ByProduct.View";
    public const string ReportsByCustomerView = "Sales.Reports.ByCustomer.View";
}
