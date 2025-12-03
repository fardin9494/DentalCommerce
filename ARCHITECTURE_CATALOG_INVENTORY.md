# معماری ارتباط بین Catalog و Inventory Contexts

## خلاصه

این سند راهنمای پیاده‌سازی ارتباط بین دو Bounded Context (Catalog و Inventory) را توضیح می‌دهد.

## معماری پیشنهادی

### اصول طراحی (Design Principles)

1. **Published Language Pattern**: استفاده از `Guid` به عنوان شناسه مشترک (`ProductId`)
2. **Anti-Corruption Layer**: استفاده از `ICatalogProductService` برای validation و lookup
3. **Loose Coupling**: هیچ foreign key constraint در دیتابیس وجود ندارد
4. **Context Independence**: هر Context دیتابیس و schema جداگانه دارد

### ساختار فعلی

```
┌─────────────────┐         ┌─────────────────┐
│  Catalog Context│         │ Inventory Context│
│                 │         │                  │
│  Schema: catalog│         │  Schema: inv     │
│  Product (Id)   │─────────│  ProductId (Guid)│
└─────────────────┘         └─────────────────┘
```

**نکته مهم**: هیچ foreign key constraint بین دو schema وجود ندارد.

## پیاده‌سازی

### 1. ثبت سرویس در DI Container

#### گزینه A: HTTP API (Microservices)

اگر Catalog و Inventory در سرویس‌های جداگانه اجرا می‌شوند:

```csharp
// در Inventory.Api/Program.cs
builder.Services.AddHttpClient<ICatalogProductService, CatalogProductHttpService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CatalogApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

#### گزینه B: Direct Database (Monolith)

اگر هر دو Context در یک Monolith هستند و از یک دیتابیس استفاده می‌کنند:

```csharp
// در Inventory.Api/Program.cs
// ابتدا Catalog DbContext را اضافه کنید
builder.Services.AddDbContext<CatalogDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("CatalogDb");
    opt.UseSqlServer(cs, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "catalog"));
});

// سپس سرویس را ثبت کنید
builder.Services.AddScoped<ICatalogProductService, CatalogProductDbService>();
```

**نکته**: برای استفاده از `CatalogProductDbService`، باید `Catalog.Infrastructure` را به `Inventory.Application` reference کنید.

### 2. استفاده در Handlers

```csharp
public sealed class AddReceiptLineHandler : IRequestHandler<AddReceiptLineCommand, Guid>
{
    private readonly InventoryDbContext _db;
    private readonly ICatalogProductService _catalogService;
    
    public AddReceiptLineHandler(
        InventoryDbContext db, 
        ICatalogProductService catalogService)
    {
        _db = db;
        _catalogService = catalogService;
    }
    
    public async Task<Guid> Handle(AddReceiptLineCommand req, CancellationToken ct)
    {
        // Validation از Catalog
        var productExists = await _catalogService.ProductExistsAsync(req.ProductId, ct);
        if (!productExists)
            throw new InvalidOperationException("محصول یافت نشد.");
        
        if (req.VariantId.HasValue)
        {
            var variantExists = await _catalogService.VariantExistsAsync(
                req.ProductId, req.VariantId.Value, ct);
            if (!variantExists)
                throw new InvalidOperationException("Variant یافت نشد.");
        }
        
        // ادامه عملیات...
        var rec = await _db.Receipts.FirstOrDefaultAsync(r => r.Id == req.ReceiptId, ct)
                  ?? throw new InvalidOperationException("رسید پیدا نشد.");
        
        var line = rec.AddLine(req.ProductId, req.VariantId, req.Qty, 
            req.LotNumber, req.ExpiryDateUtc, req.UnitCost);
        _db.Entry(line).State = EntityState.Added;
        await _db.SaveChangesAsync(ct);
        return line.Id;
    }
}
```

### 3. استفاده در FluentValidation

```csharp
public sealed class AddReceiptLineCommandValidator : AbstractValidator<AddReceiptLineCommand>
{
    private readonly ICatalogProductService _catalogService;
    
    public AddReceiptLineCommandValidator(ICatalogProductService catalogService)
    {
        _catalogService = catalogService;
        
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .MustBeValidProductId(_catalogService);
        
        RuleFor(x => x.VariantId)
            .MustBeValidVariantId(x => x.ProductId, _catalogService)
            .When(x => x.VariantId.HasValue);
    }
}
```

## مزایا

✅ **Loose Coupling**: Contexts مستقل از هم هستند  
✅ **Scalability**: می‌توان هر Context را جداگانه scale کرد  
✅ **Resilience**: خطا در یک Context باعث crash شدن دیگری نمی‌شود  
✅ **Testability**: می‌توان Mock استفاده کرد  
✅ **Flexibility**: می‌توان بین HTTP و Database implementation جابجا شد  

## نکات مهم

### ⚠️ Validation

- همیشه `ProductId` را قبل از استفاده validate کنید
- در صورت خطا در ارتباط با Catalog، تصمیم بگیرید که fail-fast کنید یا continue

### ⚠️ Performance

- برای کاهش API calls، می‌توانید caching اضافه کنید:
```csharp
builder.Services.AddMemoryCache();
// در CatalogProductService از IMemoryCache استفاده کنید
```

### ⚠️ Consistency

- در صورت حذف Product در Catalog، تصمیم بگیرید که با Inventory چه کنید:
  - **Soft Delete**: Product را در Inventory نگه دارید اما mark کنید
  - **Archive**: Inventory records را archive کنید
  - **Cascade Delete**: Inventory records را حذف کنید (⚠️ خطرناک)

### ⚠️ Error Handling

دو رویکرد برای error handling:

1. **Fail-Fast**: در صورت خطا، exception throw کنید
2. **Resilient**: در صورت خطا، log کنید و continue کنید (برای عملیات read-only)

```csharp
// Fail-Fast (پیشنهادی برای عملیات write)
if (!await _catalogService.ProductExistsAsync(productId, ct))
    throw new InvalidOperationException("محصول یافت نشد.");

// Resilient (برای عملیات read-only)
var productInfo = await _catalogService.GetProductInfoAsync(productId, ct);
if (productInfo is null)
    _logger.LogWarning("اطلاعات محصول {ProductId} یافت نشد", productId);
```

## Event-Driven (اختیاری)

برای sync کردن اطلاعات بین Contexts می‌توانید از Events استفاده کنید:

```csharp
// در Catalog Context
public class ProductDeletedEvent
{
    public Guid ProductId { get; set; }
}

// در Inventory Context
public class ProductDeletedEventHandler : INotificationHandler<ProductDeletedEvent>
{
    private readonly InventoryDbContext _db;
    
    public async Task Handle(ProductDeletedEvent notification, CancellationToken ct)
    {
        // Mark related inventory records as archived
        var stockItems = await _db.StockItems
            .Where(si => si.ProductId == notification.ProductId)
            .ToListAsync(ct);
        
        // تصمیم بگیرید چه کاری انجام دهید
        // مثلاً: Archive, Soft Delete, etc.
    }
}
```

## مثال کامل: Endpoint در Catalog API

برای استفاده از HTTP Service، باید endpoint های زیر را در Catalog API اضافه کنید:

```csharp
// در Catalog.Api/Program.cs
catalog.MapGet("/products/{id:guid}/exists", async (Guid id, CatalogDbContext db) =>
{
    var exists = await db.Products.AnyAsync(p => p.Id == id);
    return exists ? Results.Ok() : Results.NotFound();
});

catalog.MapGet("/products/{pid:guid}/variants/{vid:guid}/exists", 
    async (Guid pid, Guid vid, CatalogDbContext db) =>
{
    var exists = await db.Set<ProductVariant>()
        .AnyAsync(v => v.Id == vid && v.ProductId == pid);
    return exists ? Results.Ok() : Results.NotFound();
});
```

## خلاصه

1. ✅ از `ICatalogProductService` برای validation استفاده کنید
2. ✅ هیچ foreign key constraint اضافه نکنید
3. ✅ در Handlers، ProductId را validate کنید
4. ✅ تصمیم بگیرید که از HTTP یا Database implementation استفاده کنید
5. ✅ Error handling strategy را مشخص کنید

