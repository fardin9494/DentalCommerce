namespace Inventory.Domain.Enums;

/// <summary>
/// وضعیت موجودی کالا
/// </summary>
public enum StockStatus
{
    /// <summary>
    /// در قفسه و آزاد - قابل فروش و جابجایی
    /// </summary>
    Available = 1,
    
    /// <summary>
    /// رزرو شده - برای فروش رزرو شده
    /// </summary>
    Reserved = 2,
    
    /// <summary>
    /// مسدود - در قرنطینه (برای تایید یا بررسی)
    /// </summary>
    Blocked = 3,
    
    /// <summary>
    /// در انتظار چیده شدن - تایید شده ولی هنوز در قفسه چیده نشده
    /// </summary>
    AwaitingShelving = 4
}

