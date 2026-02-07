namespace Identity.Application.Common.Permissions;

public static class CatalogPermissions
{
    public const string Context = "Catalog";

    public static readonly IReadOnlyList<PermissionDefinition> All = new[]
    {
        new PermissionDefinition("Catalog.Products.View", "مشاهده محصولات", "مشاهده لیست و جزئیات محصولات", Context),
        new PermissionDefinition("Catalog.Products.Create", "ایجاد محصول", "ایجاد محصول جدید", Context),
        new PermissionDefinition("Catalog.Products.Basics.Edit", "ویرایش اطلاعات پایه محصول", "ویرایش مشخصات پایه محصول", Context),
        new PermissionDefinition("Catalog.Products.Description.Edit", "ویرایش توضیحات محصول", "ویرایش محتوای توضیحات محصول", Context),
        new PermissionDefinition("Catalog.Products.Seo.Edit", "ویرایش SEO محصول", "ویرایش اطلاعات سئو محصول", Context),
        new PermissionDefinition("Catalog.Products.Activate", "فعال‌سازی محصول", "فعال‌سازی محصول", Context),
        new PermissionDefinition("Catalog.Products.Hide", "مخفی‌سازی محصول", "مخفی کردن محصول", Context),
        new PermissionDefinition("Catalog.Products.ResolveBySku", "جستجو بر اساس SKU", "دریافت محصول با SKU", Context),
        new PermissionDefinition("Catalog.Products.Validate", "اعتبارسنجی محصول", "اعتبارسنجی وجود محصول/تنوع", Context),

        new PermissionDefinition("Catalog.Products.Categories.View", "مشاهده دسته‌های محصول", "مشاهده دسته‌بندی‌های محصول", Context),
        new PermissionDefinition("Catalog.Products.Categories.Edit", "ویرایش دسته‌های محصول", "تغییر دسته‌بندی‌های محصول", Context),
        new PermissionDefinition("Catalog.Products.Categories.Primary.Edit", "ویرایش دسته اصلی محصول", "تغییر دسته اصلی محصول", Context),

        new PermissionDefinition("Catalog.Products.Stores.View", "مشاهده فروشگاه‌های محصول", "مشاهده تخصیص فروشگاه‌های محصول", Context),
        new PermissionDefinition("Catalog.Products.Stores.Manage", "مدیریت فروشگاه‌های محصول", "افزودن/حذف فروشگاه‌های محصول", Context),

        new PermissionDefinition("Catalog.Products.Images.Upload", "آپلود تصویر محصول", "آپلود تصویر برای محصول", Context),
        new PermissionDefinition("Catalog.Products.Images.SetMain", "تعیین تصویر اصلی", "تعیین تصویر اصلی محصول", Context),
        new PermissionDefinition("Catalog.Products.Images.Reorder", "مرتب‌سازی تصاویر محصول", "تغییر ترتیب تصاویر محصول", Context),
        new PermissionDefinition("Catalog.Products.Images.Delete", "حذف تصویر محصول", "حذف تصاویر محصول", Context),

        new PermissionDefinition("Catalog.Products.Variation.Edit", "ویرایش کلید تنوع", "تنظیم کلید تنوع محصول", Context),
        new PermissionDefinition("Catalog.Products.Variants.View", "مشاهده تنوع‌ها", "مشاهده تنوع‌های محصول", Context),
        new PermissionDefinition("Catalog.Products.Variants.Create", "ایجاد تنوع", "افزودن تنوع جدید محصول", Context),
        new PermissionDefinition("Catalog.Products.Variants.Edit", "ویرایش تنوع", "ویرایش اطلاعات تنوع محصول", Context),
        new PermissionDefinition("Catalog.Products.Variants.Delete", "حذف تنوع", "حذف تنوع محصول", Context),

        new PermissionDefinition("Catalog.Products.Properties.View", "مشاهده ویژگی‌های محصول", "مشاهده ویژگی‌های محصول", Context),
        new PermissionDefinition("Catalog.Products.Properties.Create", "ایجاد/ویرایش ویژگی محصول", "افزودن یا ویرایش ویژگی محصول", Context),
        new PermissionDefinition("Catalog.Products.Properties.Delete", "حذف ویژگی محصول", "حذف ویژگی محصول", Context),

        new PermissionDefinition("Catalog.Categories.Tree.View", "مشاهده درخت دسته‌بندی", "مشاهده ساختار درختی دسته‌بندی‌ها", Context),
        new PermissionDefinition("Catalog.Categories.Leaves.View", "مشاهده برگ‌های دسته‌بندی", "مشاهده دسته‌های سطح آخر", Context),
        new PermissionDefinition("Catalog.Categories.LeavesWithProducts.View", "مشاهده برگ‌های دارای محصول", "مشاهده دسته‌های سطح آخر دارای محصول", Context),
        new PermissionDefinition("Catalog.Categories.Flags.View", "مشاهده فلگ دسته‌بندی", "مشاهده وضعیت اتصال دسته به محصول", Context),
        new PermissionDefinition("Catalog.Categories.Create", "ایجاد دسته‌بندی", "ایجاد دسته‌بندی جدید", Context),
        new PermissionDefinition("Catalog.Categories.Rename", "تغییر نام دسته‌بندی", "ویرایش نام/اسلاگ دسته‌بندی", Context),
        new PermissionDefinition("Catalog.Categories.Move", "جابجایی دسته‌بندی", "جابجایی دسته‌بندی در درخت", Context),

        new PermissionDefinition("Catalog.Brands.View", "مشاهده برندها", "مشاهده لیست برندها", Context),
        new PermissionDefinition("Catalog.Brands.Detail.View", "مشاهده جزئیات برند", "مشاهده جزئیات برند", Context),
        new PermissionDefinition("Catalog.Brands.Create", "ایجاد برند", "ایجاد برند جدید", Context),
        new PermissionDefinition("Catalog.Brands.Edit", "ویرایش برند", "ویرایش اطلاعات برند", Context),
        new PermissionDefinition("Catalog.Brands.Rename", "تغییر نام برند", "تغییر نام برند", Context),
        new PermissionDefinition("Catalog.Brands.Profile.Edit", "ویرایش پروفایل برند", "ویرایش پروفایل، خلاصه و توضیحات برند", Context),
        new PermissionDefinition("Catalog.Brands.Status.Edit", "ویرایش وضعیت برند", "تغییر وضعیت برند", Context),
        new PermissionDefinition("Catalog.Brands.Logo.Upload", "آپلود لوگوی برند", "آپلود یا تغییر لوگوی برند", Context),
        new PermissionDefinition("Catalog.Brands.Delete", "حذف برند", "حذف برند", Context),
        new PermissionDefinition("Catalog.Brands.Aliases.View", "مشاهده نام‌های جایگزین برند", "مشاهده لیست نام‌های جایگزین برند", Context),
        new PermissionDefinition("Catalog.Brands.Aliases.Manage", "مدیریت نام‌های جایگزین برند", "افزودن/حذف نام‌های جایگزین برند", Context),

        new PermissionDefinition("Catalog.Stores.View", "مشاهده فروشگاه‌ها", "مشاهده لیست فروشگاه‌ها", Context),
        new PermissionDefinition("Catalog.Stores.Detail.View", "مشاهده جزئیات فروشگاه", "مشاهده جزئیات فروشگاه", Context),
        new PermissionDefinition("Catalog.Stores.Create", "ایجاد فروشگاه", "ایجاد فروشگاه جدید", Context),
        new PermissionDefinition("Catalog.Stores.Rename", "تغییر نام فروشگاه", "تغییر نام فروشگاه", Context),
        new PermissionDefinition("Catalog.Stores.Domain.Edit", "ویرایش دامنه فروشگاه", "ویرایش دامنه/وب‌سایت فروشگاه", Context),

        new PermissionDefinition("Catalog.Countries.View", "مشاهده کشورها", "مشاهده لیست کشورها", Context),
        new PermissionDefinition("Catalog.Countries.Create", "ایجاد کشور", "ایجاد کشور جدید", Context),
        new PermissionDefinition("Catalog.Countries.Edit", "ویرایش کشور", "ویرایش کشور", Context),
        new PermissionDefinition("Catalog.Countries.Delete", "حذف کشور", "حذف کشور", Context),

        new PermissionDefinition("Catalog.Metadata.PropertyKeys.View", "مشاهده کلیدهای ویژگی", "مشاهده کلیدهای پرتکرار ویژگی محصول", Context),
        new PermissionDefinition("Catalog.Metadata.PropertyValues.View", "مشاهده مقادیر ویژگی", "مشاهده مقادیر پرتکرار برای یک کلید ویژگی", Context),
        new PermissionDefinition("Catalog.Metadata.VariantValues.View", "مشاهده مقادیر تنوع", "مشاهده مقادیر پرتکرار تنوع", Context),
        new PermissionDefinition("Catalog.Metadata.VariantRecent.View", "مشاهده تنوع‌های اخیر", "مشاهده مقادیر و SKUهای اخیر تنوع", Context),
    };

    public static bool IsValid(string key) => All.Any(x => x.Key == key);
}
