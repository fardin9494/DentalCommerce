import { Link, Outlet, useLocation } from 'react-router-dom'
import { LoginPage } from '../../app/LoginPage'
import { useAdminAuth } from '../../app/auth'
import { PermissionGate } from '@/app/permissions'
import { PricingPermissionKeys } from '@/app/pricingPermissionKeys'

export function Layout() {
  const loc = useLocation()
  const { token, logout } = useAdminAuth()

  if (!token) {
    return <LoginPage />
  }

  return (
    <div className="min-h-screen flex flex-col">
      <header className="bg-white border-b">
        <div className="container-std flex items-center justify-between h-14">
          <Link to="/" className="font-semibold">مدیریت قیمت‌گذاری</Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/pricing/home" className={navCls(loc.pathname.startsWith('/pricing/home'))}>راهنما</Link>
            <PermissionGate anyPermissions={[PricingPermissionKeys.PoliciesListView, PricingPermissionKeys.PoliciesView, PricingPermissionKeys.PoliciesEdit]}>
              <Link to="/pricing/policies" className={navCls(loc.pathname.startsWith('/pricing/policies'))}>سیاست قیمت‌گذاری</Link>
            </PermissionGate>
            <PermissionGate permission={PricingPermissionKeys.PriceListsView}>
              <Link to="/pricing/pricelists" className={navCls(loc.pathname.startsWith('/pricing/pricelists'))}>لیست قیمت‌ها</Link>
            </PermissionGate>
            <PermissionGate permission={PricingPermissionKeys.OverridesView}>
              <Link to="/pricing/overrides" className={navCls(loc.pathname.startsWith('/pricing/overrides'))}>قیمت‌های ویژه</Link>
            </PermissionGate>
            <PermissionGate permission={PricingPermissionKeys.CampaignsView}>
              <Link to="/pricing/campaigns" className={navCls(loc.pathname.startsWith('/pricing/campaigns'))}>کمپین‌ها</Link>
            </PermissionGate>
            <PermissionGate permission={PricingPermissionKeys.CouponsView}>
              <Link to="/pricing/coupons" className={navCls(loc.pathname.startsWith('/pricing/coupons'))}>کدهای تخفیف</Link>
            </PermissionGate>
            <PermissionGate anyPermissions={[PricingPermissionKeys.BulkPricesUpdate, PricingPermissionKeys.BulkCampaignsCategoryCreate]}>
              <Link to="/pricing/bulk" className={navCls(loc.pathname.startsWith('/pricing/bulk'))}>عملیات گروهی</Link>
            </PermissionGate>
            <PermissionGate permission={PricingPermissionKeys.QuotesCreate}>
              <Link to="/pricing/quote" className={navCls(loc.pathname.startsWith('/pricing/quote'))}>پیش‌نمایش قیمت</Link>
            </PermissionGate>
            <PermissionGate anyPermissions={[
              PricingPermissionKeys.PriceListsView,
              PricingPermissionKeys.PriceListsEdit,
              PricingPermissionKeys.OverridesView,
              PricingPermissionKeys.OverridesCreate,
              PricingPermissionKeys.CampaignsView,
              PricingPermissionKeys.CampaignsCreate,
            ]}>
              <Link to="/pricing/product" className={navCls(loc.pathname.startsWith('/pricing/product'))}>قیمت‌گذاری محصول</Link>
            </PermissionGate>
            <PermissionGate anyPermissions={[PricingPermissionKeys.PriceListsView, PricingPermissionKeys.OverridesView, PricingPermissionKeys.CampaignsView]}>
              <Link to="/pricing/report" className={navCls(loc.pathname.startsWith('/pricing/report'))}>گزارش قیمت‌ها</Link>
            </PermissionGate>
            <button onClick={logout} className="px-3 py-1.5 rounded-md hover:bg-gray-100 text-sm">
              خروج
            </button>
          </nav>
        </div>
      </header>
      <main className="container-std py-6 flex-1 w-full">
        <Outlet />
      </main>
      <footer className="border-t bg-white">
        <div className="container-std py-3 text-xs text-gray-500">ساخته شده توسط wira dev</div>
      </footer>
    </div>
  )
}

function navCls(active: boolean) {
  return `px-3 py-1.5 rounded-md hover:bg-gray-100 ${active ? 'bg-gray-900 text-white hover:bg-gray-800' : ''}`
}
