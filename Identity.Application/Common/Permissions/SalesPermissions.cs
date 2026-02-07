namespace Identity.Application.Common.Permissions;

public static class SalesPermissions
{
    public const string Context = "Sales";

    public static readonly IReadOnlyList<PermissionDefinition> All = new[]
    {
        new PermissionDefinition("Sales.Orders.View", "View Orders", "View the orders list", Context),
        new PermissionDefinition("Sales.Orders.Detail.View", "View Order Details", "View detailed order information", Context),
        new PermissionDefinition("Sales.Orders.Timeline.View", "View Order Timeline", "View order timeline and status changes", Context),
        new PermissionDefinition("Sales.Orders.Create", "Create Order", "Create an order record directly", Context),
        new PermissionDefinition("Sales.Orders.Ship", "Ship Order", "Ship orders", Context),
        new PermissionDefinition("Sales.Orders.Deliver", "Deliver Order", "Mark orders as delivered", Context),
        new PermissionDefinition("Sales.Orders.Return", "Return Order", "Mark orders as returned", Context),
        new PermissionDefinition("Sales.Orders.Refund", "Refund Order", "Run direct refund for an order", Context),
        new PermissionDefinition("Sales.Orders.RetryPayment", "Retry Payment", "Retry failed order payments", Context),
        new PermissionDefinition("Sales.Orders.Cancel", "Cancel Order", "Cancel entire orders", Context),
        new PermissionDefinition("Sales.Orders.CancelLines", "Cancel Order Lines", "Cancel specific order lines", Context),
        new PermissionDefinition("Sales.Orders.Notes.View", "View Order Notes", "View order notes", Context),
        new PermissionDefinition("Sales.Orders.Notes.Add", "Add Order Note", "Add order notes", Context),

        new PermissionDefinition("Sales.Refunds.View", "View Refunds", "View order refund requests", Context),
        new PermissionDefinition("Sales.Refunds.Request", "Request Refund", "Create a refund request for an order", Context),
        new PermissionDefinition("Sales.Refunds.Approve", "Approve Refund", "Approve refund requests", Context),
        new PermissionDefinition("Sales.Refunds.Reject", "Reject Refund", "Reject refund requests", Context),
        new PermissionDefinition("Sales.Refunds.Complete", "Complete Refund", "Complete approved refunds", Context),

        new PermissionDefinition("Sales.Reports.Daily.View", "View Daily Report", "View daily sales report", Context),
        new PermissionDefinition("Sales.Reports.Monthly.View", "View Monthly Report", "View monthly sales report", Context),
        new PermissionDefinition("Sales.Reports.BySite.View", "View Site Report", "View sales report grouped by site", Context),
        new PermissionDefinition("Sales.Reports.ByProduct.View", "View Product Report", "View sales report grouped by product", Context),
        new PermissionDefinition("Sales.Reports.ByCustomer.View", "View Customer Report", "View sales report grouped by customer", Context),
    };

    public static bool IsValid(string key) => All.Any(x => x.Key == key);
}
