# ارتباط بین Catalog و Inventory Contexts

## معماری پیشنهادی

در این پروژه از **Published Language Pattern** و **Anti-Corruption Layer** برای ارتباط بین دو Bounded Context استفاده می‌شود.

### اصول طراحی

1. **استقلال Contexts**: هر Context دیتابیس و schema جداگانه دارد
2. **Published Language**: استفاده از `Guid` به عنوان شناسه مشترک (ProductId)
3. **Anti-Corruption Layer**: استفاده از `ICatalogProductService` برای validation
4. **عدم Foreign Key**: هیچ foreign key constraint در دیتابیس وجود ندارد

### رویکردهای پیاده‌سازی

#### رویکرد 1: HTTP API (Microservices / Distributed)

اگر Catalog و Inventory در سرویس‌های جداگانه اجرا می‌شوند:

```csharp
// در Program.cs
builder.Services.AddHttpClient<ICatalogProductService, CatalogProductService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogApi:BaseUrl"]!);
});
```

#### رویکرد 2: Direct Database Access (Monolith)

اگر هر دو Context در یک Monolith هستند و می‌خواهید از دیتابیس مستقیماً استفاده کنید:

```csharp
// در Inventory.Application/Common/CatalogProductService.cs
public sealed class CatalogProductService : ICatalogProductService
{
    private readonly CatalogDbContext _catalogDb;
    
    public async Task<bool> ProductExistsAsync(Guid productId, CancellationToken ct = default)
    {
        return await _catalogDb.Set<Catalog.Domain.Products.Product>()
            .AnyAsync(p => p.Id == productId, ct);
    }
}
```

#### رویکرد 3: Event-Driven (اختیاری - برای Sync)

برای sync کردن اطلاعات بین Contexts می‌توانید از Events استفاده کنید:

```csharp
// وقتی Product در Catalog حذف می‌شود
public class ProductDeletedEvent
{
    public Guid ProductId { get; set; }
}

// در Inventory Context
public class ProductDeletedEventHandler : INotificationHandler<ProductDeletedEvent>
{
    // Mark related inventory records as invalid
}
```

### استفاده در Handlers

```csharp
public sealed class AddReceiptLineHandler : IRequestHandler<AddReceiptLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogProductService _catalogService;
    
    public async Task<Guid> Handle(AddReceiptLineCommand req, CancellationToken ct)
    {
        // Validation از Catalog
        var productExists = await _catalogService.ProductExistsAsync(req.ProductId, ct);
        if (!productExists)
            throw new InvalidOperationException("محصول یافت نشد.");
        
        if (req.VariantId.HasValue)
        {
            var variantExists = await _catalogService.VariantExistsAsync(req.ProductId, req.VariantId.Value, ct);
            if (!variantExists)
                throw new InvalidOperationException("Variant یافت نشد.");
        }
        
        // ادامه عملیات...
    }
}
```

### مزایا

✅ **Loose Coupling**: Contexts مستقل از هم هستند  
✅ **Scalability**: می‌توان هر Context را جداگانه scale کرد  
✅ **Resilience**: خطا در یک Context باعث crash شدن دیگری نمی‌شود  
✅ **Testability**: می‌توان Mock استفاده کرد  

### نکات مهم

⚠️ **Validation**: همیشه ProductId را قبل از استفاده validate کنید  
⚠️ **Error Handling**: در صورت خطا در ارتباط با Catalog، تصمیم بگیرید که fail-fast کنید یا continue  
⚠️ **Performance**: برای کاهش API calls، می‌توانید caching اضافه کنید  
⚠️ **Consistency**: در صورت حذف Product در Catalog، تصمیم بگیرید که با Inventory چه کنید (Soft Delete, Archive, etc.)

