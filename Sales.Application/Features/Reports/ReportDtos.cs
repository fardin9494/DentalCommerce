namespace Sales.Application.Features.Reports;

public sealed class DailySalesReportItem
{
    public DateTime Date { get; init; }
    public int OrdersCount { get; init; }
    public int ItemsCount { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal FinalTotal { get; init; }
    public decimal CashbackTotal { get; init; }
}

public sealed class MonthlySalesReportItem
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int OrdersCount { get; init; }
    public int ItemsCount { get; init; }
    public decimal Subtotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal FinalTotal { get; init; }
    public decimal CashbackTotal { get; init; }
}

public sealed class SiteSalesReportItem
{
    public Guid SiteId { get; init; }
    public string? SiteName { get; init; }
    public string? SiteDomain { get; init; }
    public int OrdersCount { get; init; }
    public int ItemsCount { get; init; }
    public decimal FinalTotal { get; init; }
}

public sealed class ProductSalesReportItem
{
    public string SkuId { get; init; } = null!;
    public int OrdersCount { get; init; }
    public int Quantity { get; init; }
    public decimal Revenue { get; init; }
}

public sealed class CustomerSalesReportItem
{
    public Guid? UserId { get; init; }
    public int OrdersCount { get; init; }
    public decimal FinalTotal { get; init; }
}
