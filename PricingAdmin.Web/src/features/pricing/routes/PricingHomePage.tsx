import { Link } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'

export function PricingHomePage() {
  return (
    <div className="space-y-4">
      <PageHeader title="راهنمای قیمت‌گذاری">
        این بخش به شما کمک می‌کند سریع بفهمید قیمت‌ها کجا ذخیره می‌شوند، هر صفحه چه کاری می‌کند، و برای قیمت‌گذاری یک محصول از کدام مسیر استفاده کنید.
      </PageHeader>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <div className="card p-4 space-y-3">
          <h3 className="font-semibold">مسیر پیشنهادی (ساده‌ترین)</h3>
          <ol className="list-decimal pr-5 space-y-2 text-sm text-gray-700">
            <li>
              <Link className="text-gray-900 underline" to="/pricing/product">قیمت‌گذاری محصول</Link>
              <span> — جستجوی محصول از کاتالوگ، انتخاب واریانت، ثبت قیمت پایه و قیمت سایت‌ها.</span>
            </li>
            <li>
              <Link className="text-gray-900 underline" to="/pricing/quote">پیش‌نمایش قیمت</Link>
              <span> — یک سبد تست بسازید و خروجی «قیمت نهایی + علت‌ها» را ببینید.</span>
            </li>
            <li>
              <Link className="text-gray-900 underline" to="/pricing/policies">سیاست قیمت‌گذاری</Link>
              <span> — اگر نتیجه تجمیع تخفیف‌ها عجیب شد، مدل تجمیع را بررسی کنید.</span>
            </li>
          </ol>
          <div className="text-xs text-gray-600">
            نکته: اگر محصول واریانت دارد، قیمت روی «واریانت‌ها» ثبت می‌شود؛ اگر واریانت ندارد، قیمت روی «خود محصول» ثبت می‌شود.
          </div>
        </div>

        <div className="card p-4 space-y-3">
          <h3 className="font-semibold">قیمت چگونه محاسبه می‌شود؟ (ترتیب)</h3>
          <ol className="list-decimal pr-5 space-y-2 text-sm text-gray-700">
            <li>قیمت پایه از «لیست قیمت فعال» خوانده می‌شود.</li>
            <li>اگر قیمت ویژه سایت/کاربر وجود داشته باشد، روی قیمت پایه اعمال می‌شود (اول کاربر، بعد سایت؛ priority بالاتر مقدم است).</li>
            <li>کمپین‌ها (تخفیف/باندل/کش‌بک/…) ارزیابی می‌شوند و طبق سیاست تجمیع انتخاب و اعمال می‌شوند.</li>
            <li>اگر کد تخفیف وارد شده باشد و مجاز باشد، اعمال می‌شود.</li>
            <li>گاردریل‌ها (سقف تخفیف و کف قیمت) اعمال می‌شوند و اگر لازم باشد ابتدا کوپن/کمپین‌ها حذف یا محدود می‌شوند.</li>
          </ol>
          <div className="text-xs text-gray-600">
            برای دیدن دلیل دقیق انتخاب/رد شدن‌ها از «پیش‌نمایش قیمت» و بخش Trace استفاده کنید.
          </div>
        </div>
      </div>

      <div className="card p-4 space-y-3">
        <h3 className="font-semibold">هر صفحه به چه درد می‌خورد؟</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm">
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/product">قیمت‌گذاری محصول</Link>
            </div>
            <div className="text-gray-700 mt-1">کار روزمره: انتخاب محصول/واریانت و ثبت قیمت پایه + قیمت سایت‌ها + یک تخفیف ساده برای همان SKU.</div>
          </div>
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/pricelists">لیست قیمت‌ها</Link>
            </div>
            <div className="text-gray-700 mt-1">مدیریت لیست قیمت فعال و قیمت‌های پله‌ای (عمدتاً برای کارهای انبوه یا تیراژ).</div>
          </div>
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/overrides">قیمت‌های ویژه</Link>
            </div>
            <div className="text-gray-700 mt-1">قیمت ویژه سطح «سایت» یا «کاربر» برای یک SKU (درصد/ریالی/قیمت ثابت + بازه زمانی + priority).</div>
          </div>
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/campaigns">کمپین‌ها</Link>
            </div>
            <div className="text-gray-700 mt-1">تخفیف‌های مارکتینگ: شرط‌ها (Eligibility) + مزیت‌ها (Benefit) + تجمیع/انحصار + گاردریل.</div>
          </div>
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/coupons">کدهای تخفیف</Link>
            </div>
            <div className="text-gray-700 mt-1">کوپن‌ها با شرط/مزیت و محدودیت استفاده (کل/هر کاربر) + قابلیت ترکیب با کمپین‌ها.</div>
          </div>
          <div className="border rounded-md p-3">
            <div className="font-semibold">
              <Link className="underline" to="/pricing/bulk">عملیات گروهی</Link>
            </div>
            <div className="text-gray-700 mt-1">به‌روزرسانی گروهی قیمت‌ها و ساخت کمپین دسته‌بندی (Paste/Preview/Submit).</div>
          </div>
        </div>
      </div>

      <div className="card p-4 space-y-3">
        <h3 className="font-semibold">قیمت‌ها «کجا» ذخیره می‌شوند؟</h3>
        <div className="overflow-x-auto">
          <table className="min-w-full text-sm text-center">
            <thead>
              <tr className="bg-gray-100">
                <th className="p-2">نوع داده</th>
                <th className="p-2">کجا ذخیره می‌شود</th>
                <th className="p-2">از کجا در UI می‌بینید/ویرایش می‌کنید</th>
              </tr>
            </thead>
            <tbody>
              <Row
                kind="قیمت پایه (Global)"
                storage="PriceLists / PriceListItems"
                ui={<Link className="underline" to="/pricing/product">قیمت‌گذاری محصول</Link>}
              />
              <Row
                kind="قیمت ویژه سایت/کاربر"
                storage="PriceOverrides"
                ui={<Link className="underline" to="/pricing/overrides">قیمت‌های ویژه</Link>}
              />
              <Row
                kind="کمپین (Promotion)"
                storage="PromotionCampaigns"
                ui={<Link className="underline" to="/pricing/campaigns">کمپین‌ها</Link>}
              />
              <Row
                kind="کوپن"
                storage="Coupons (+ CouponRedemptions برای شمارش)"
                ui={<Link className="underline" to="/pricing/coupons">کدهای تخفیف</Link>}
              />
              <Row
                kind="سیاست تجمیع تخفیف‌ها"
                storage="PricingPolicies (Global/Site)"
                ui={<Link className="underline" to="/pricing/policies">سیاست قیمت‌گذاری</Link>}
              />
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

function Row(props: { kind: string; storage: string; ui: React.ReactNode }) {
  return (
    <tr className="border-b last:border-0">
      <td className="p-2">{props.kind}</td>
      <td className="p-2">{props.storage}</td>
      <td className="p-2">{props.ui}</td>
    </tr>
  )
}

