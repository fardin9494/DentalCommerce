namespace Sales.Domain.Orders;

public enum OrderStatus
{
    Draft = 0,
    Placed = 1,
    PaymentFailed = 2,
    Cancelled = 3,
    Shipped = 4,
    Delivered = 5,
    Returned = 6,
    Refunded = 7
}
