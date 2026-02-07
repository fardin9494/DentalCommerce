namespace Identity.Application.Common.Permissions;

public static class InventoryPermissions
{
    public const string Context = "Inventory";

    public static readonly IReadOnlyList<PermissionDefinition> All = new[]
    {
        new PermissionDefinition("Inventory.Dashboard.View", "مشاهده داشبورد", "مشاهده شاخص‌ها و آمار انبار", Context),

        new PermissionDefinition("Inventory.Receipts.View", "مشاهده رسیدها", "مشاهده لیست و جزئیات رسیدها", Context),
        new PermissionDefinition("Inventory.Receipts.Create", "ایجاد رسید", "ایجاد رسید جدید", Context),
        new PermissionDefinition("Inventory.Receipts.Edit", "ویرایش رسید", "ویرایش اطلاعات رسید", Context),
        new PermissionDefinition("Inventory.Receipts.Receive", "دریافت رسید", "ثبت دریافت رسید", Context),
        new PermissionDefinition("Inventory.Receipts.Approve", "تایید رسید", "تایید رسید یا اقلام رسید", Context),
        new PermissionDefinition("Inventory.Receipts.Reject", "رد رسید", "رد رسید یا اقلام رسید", Context),
        new PermissionDefinition("Inventory.Receipts.Cancel", "لغو رسید", "لغو رسید", Context),

        new PermissionDefinition("Inventory.ReceiptRejections.View", "مشاهده اقلام رد شده", "مشاهده اقلام رد شده رسید", Context),
        new PermissionDefinition("Inventory.ReceiptRejections.Resolve", "تعیین تکلیف اقلام رد شده", "تعیین تکلیف اقلام رد شده رسید", Context),

        new PermissionDefinition("Inventory.Issues.View", "مشاهده خروجی‌ها", "مشاهده لیست و جزئیات خروجی‌ها", Context),
        new PermissionDefinition("Inventory.Issues.Create", "ایجاد خروجی", "ایجاد خروجی جدید", Context),
        new PermissionDefinition("Inventory.Issues.Edit", "ویرایش خروجی", "ویرایش اطلاعات خروجی", Context),
        new PermissionDefinition("Inventory.Issues.Allocate", "تخصیص خروجی", "تخصیص موجودی و سریال برای خروجی", Context),
        new PermissionDefinition("Inventory.Issues.Post", "ثبت خروجی", "ثبت و نهایی‌سازی خروجی", Context),
        new PermissionDefinition("Inventory.Issues.Cancel", "لغو خروجی", "لغو خروجی", Context),

        new PermissionDefinition("Inventory.Transfers.View", "مشاهده انتقالات", "مشاهده لیست و جزئیات انتقالات", Context),
        new PermissionDefinition("Inventory.Transfers.Create", "ایجاد انتقال", "ایجاد انتقال جدید", Context),
        new PermissionDefinition("Inventory.Transfers.Edit", "ویرایش انتقال", "ویرایش اطلاعات انتقال", Context),
        new PermissionDefinition("Inventory.Transfers.Allocate", "تخصیص انتقال", "تخصیص موجودی و سریال برای انتقال", Context),
        new PermissionDefinition("Inventory.Transfers.Ship", "ارسال انتقال", "ارسال انتقال بین انبارها", Context),
        new PermissionDefinition("Inventory.Transfers.Receive", "دریافت انتقال", "دریافت انتقال", Context),
        new PermissionDefinition("Inventory.Transfers.Complete", "تکمیل انتقال", "تکمیل و بستن انتقال", Context),
        new PermissionDefinition("Inventory.Transfers.Cancel", "لغو انتقال", "لغو انتقال", Context),

        new PermissionDefinition("Inventory.Adjustments.View", "مشاهده اصلاحات", "مشاهده لیست و جزئیات اصلاحات", Context),
        new PermissionDefinition("Inventory.Adjustments.Create", "ایجاد اصلاح", "ایجاد اصلاح موجودی", Context),
        new PermissionDefinition("Inventory.Adjustments.Edit", "ویرایش اصلاح", "ویرایش اصلاح موجودی", Context),
        new PermissionDefinition("Inventory.Adjustments.Post", "ثبت اصلاح", "ثبت و نهایی‌سازی اصلاح موجودی", Context),
        new PermissionDefinition("Inventory.Adjustments.Cancel", "لغو اصلاح", "لغو اصلاح موجودی", Context),

        new PermissionDefinition("Inventory.Costs.View", "مشاهده هزینه", "مشاهده هزینه و قیمت موجودی", Context),
        new PermissionDefinition("Inventory.Costs.Edit", "ویرایش هزینه", "ویرایش هزینه و قیمت موجودی", Context),

        new PermissionDefinition("Inventory.Shelves.View", "مشاهده قفسه‌ها", "مشاهده قفسه‌ها", Context),
        new PermissionDefinition("Inventory.Shelves.Manage", "مدیریت قفسه‌ها", "ایجاد/ویرایش/حذف قفسه‌ها", Context),
        new PermissionDefinition("Inventory.Operations.MoveStock", "جابجایی کالا", "جابجایی کالا بین قفسه‌ها", Context),

        new PermissionDefinition("Inventory.Warehouse.View", "مشاهده انبارها", "مشاهده انبارها", Context),
        new PermissionDefinition("Inventory.Warehouse.Create", "ایجاد انبار", "ایجاد انبار جدید", Context),
        new PermissionDefinition("Inventory.Warehouse.Edit", "ویرایش انبار", "ویرایش اطلاعات انبار", Context),
        new PermissionDefinition("Inventory.Warehouse.Activate", "فعال/غیرفعال کردن انبار", "فعال یا غیرفعال کردن انبار", Context),

        new PermissionDefinition("Inventory.StockLedger.View", "مشاهده کاردکس", "مشاهده کاردکس موجودی", Context),
        new PermissionDefinition("Inventory.StockItems.View", "مشاهده موجودی‌ها", "مشاهده لیست موجودی‌ها", Context),
        new PermissionDefinition("Inventory.StockItems.Serials.View", "مشاهده سریال‌ها", "مشاهده سریال‌های موجودی", Context),
        new PermissionDefinition("Inventory.StockItems.Unassigned.View", "مشاهده موجودی‌های بدون قفسه", "مشاهده موجودی‌های بدون قفسه", Context),
        new PermissionDefinition("Inventory.StockItems.Products.View", "مشاهده محصولات موجودی", "مشاهده محصولات موجودی", Context),

        new PermissionDefinition("Inventory.Reservations.View", "مشاهده رزروها", "مشاهده رزروهای موجودی", Context),
        new PermissionDefinition("Inventory.Reservations.Manage", "مدیریت رزروها", "ایجاد/آزادسازی رزروهای موجودی", Context),

        new PermissionDefinition("Inventory.Catalog.Proxy", "دسترسی به پروکسی کاتالوگ", "دسترسی به پروکسی کاتالوگ", Context),
        new PermissionDefinition("Inventory.Reports.View", "مشاهده گزارش‌ها", "مشاهده گزارش‌های انبار", Context),
        new PermissionDefinition("Inventory.Audit.View", "مشاهده لاگ‌ها", "مشاهده لاگ رویدادهای انبار", Context),
    };

    public static bool IsValid(string key) => All.Any(x => x.Key == key);
}
