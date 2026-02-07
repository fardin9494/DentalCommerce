namespace Identity.Application.Common.Permissions;

public static class PricingPermissions
{
    public const string Context = "Pricing";

    public static readonly IReadOnlyList<PermissionDefinition> All = new[]
    {
        new PermissionDefinition("Pricing.Quotes.Create", "استعلام قیمت", "محاسبه و دریافت پیش‌فاکتور قیمت", Context),

        new PermissionDefinition("Pricing.PriceLists.View", "مشاهده لیست‌های قیمت", "مشاهده لیست لیست‌های قیمت", Context),
        new PermissionDefinition("Pricing.PriceLists.Detail.View", "مشاهده جزئیات لیست قیمت", "مشاهده جزئیات یک لیست قیمت", Context),
        new PermissionDefinition("Pricing.PriceLists.Create", "ایجاد لیست قیمت", "ایجاد لیست قیمت جدید", Context),
        new PermissionDefinition("Pricing.PriceLists.Edit", "ویرایش لیست قیمت", "ویرایش اطلاعات لیست قیمت", Context),
        new PermissionDefinition("Pricing.PriceLists.Delete", "حذف لیست قیمت", "حذف لیست قیمت", Context),

        new PermissionDefinition("Pricing.Overrides.View", "مشاهده اوررایدها", "مشاهده لیست اوررایدهای قیمت", Context),
        new PermissionDefinition("Pricing.Overrides.Detail.View", "مشاهده جزئیات اورراید", "مشاهده جزئیات اورراید قیمت", Context),
        new PermissionDefinition("Pricing.Overrides.Create", "ایجاد اورراید", "ایجاد اورراید قیمت", Context),
        new PermissionDefinition("Pricing.Overrides.Edit", "ویرایش اورراید", "ویرایش اورراید قیمت", Context),
        new PermissionDefinition("Pricing.Overrides.Delete", "حذف اورراید", "حذف اورراید قیمت", Context),

        new PermissionDefinition("Pricing.Campaigns.View", "مشاهده کمپین‌ها", "مشاهده لیست کمپین‌های تخفیف", Context),
        new PermissionDefinition("Pricing.Campaigns.Detail.View", "مشاهده جزئیات کمپین", "مشاهده جزئیات کمپین تخفیف", Context),
        new PermissionDefinition("Pricing.Campaigns.Create", "ایجاد کمپین", "ایجاد کمپین تخفیف", Context),
        new PermissionDefinition("Pricing.Campaigns.Edit", "ویرایش کمپین", "ویرایش کمپین تخفیف", Context),
        new PermissionDefinition("Pricing.Campaigns.Delete", "حذف کمپین", "حذف کمپین تخفیف", Context),

        new PermissionDefinition("Pricing.Coupons.View", "مشاهده کوپن‌ها", "مشاهده لیست کوپن‌ها", Context),
        new PermissionDefinition("Pricing.Coupons.Detail.View", "مشاهده جزئیات کوپن", "مشاهده جزئیات کوپن", Context),
        new PermissionDefinition("Pricing.Coupons.Usage.View", "مشاهده مصرف کوپن", "مشاهده آمار و سوابق مصرف کوپن", Context),
        new PermissionDefinition("Pricing.Coupons.Reservations.View", "مشاهده رزروهای کوپن", "مشاهده رزروهای کوپن", Context),
        new PermissionDefinition("Pricing.Coupons.Create", "ایجاد کوپن", "ایجاد کوپن جدید", Context),
        new PermissionDefinition("Pricing.Coupons.Reserve", "رزرو کوپن", "رزرو کوپن برای کاربر/سبد", Context),
        new PermissionDefinition("Pricing.Coupons.Edit", "ویرایش کوپن", "ویرایش اطلاعات کوپن", Context),
        new PermissionDefinition("Pricing.Coupons.Delete", "حذف کوپن", "حذف کوپن", Context),

        new PermissionDefinition("Pricing.Bulk.Prices.Update", "به‌روزرسانی گروهی قیمت", "به‌روزرسانی گروهی قیمت محصولات", Context),
        new PermissionDefinition("Pricing.Bulk.Campaigns.Category.Create", "ایجاد گروهی کمپین دسته", "ایجاد گروهی کمپین بر اساس دسته", Context),

        new PermissionDefinition("Pricing.Policies.List.View", "مشاهده لیست سیاست‌ها", "مشاهده لیست سیاست‌های قیمت‌گذاری", Context),
        new PermissionDefinition("Pricing.Policies.View", "مشاهده سیاست قیمت‌گذاری", "مشاهده سیاست قیمت‌گذاری سایت", Context),
        new PermissionDefinition("Pricing.Policies.Edit", "ویرایش سیاست قیمت‌گذاری", "ایجاد/ویرایش سیاست قیمت‌گذاری سایت", Context),
    };

    public static bool IsValid(string key) => All.Any(x => x.Key == key);
}
