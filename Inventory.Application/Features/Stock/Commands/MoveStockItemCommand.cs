using MediatR;

namespace Inventory.Application.Features.Stock.Commands;

public sealed class MoveStockItemCommand : IRequest<Unit>
{
    /// <summary>
    /// شناسه رکوردی که کالا از آن برداشته می‌شود (مبدا)
    /// </summary>
    public Guid SourceStockItemId { get; set; }

    /// <summary>
    /// شناسه قفسه‌ای که کالا باید به آن منتقل شود (مقصد)
    /// </summary>
    public Guid TargetShelfId { get; set; }

    /// <summary>
    /// تعداد کالایی که جابجا می‌شود
    /// </summary>
    public decimal Qty { get; set; }

    /// <summary>
    /// یادداشت اختیاری برای ثبت در کاردکس (مثلاً دلیل جابجایی)
    /// </summary>
    public string? Note { get; set; }
}