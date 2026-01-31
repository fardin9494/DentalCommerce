import { Link, Outlet, useLocation } from 'react-router-dom'
import { LoginPage } from '../../app/LoginPage'
import { useAdminAuth } from '../../app/auth'

export function Layout() {
  const loc = useLocation()
  const { token } = useAdminAuth()

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
            <Link to="/pricing/policies" className={navCls(loc.pathname.startsWith('/pricing/policies'))}>سیاست قیمت‌گذاری</Link>
            <Link to="/pricing/pricelists" className={navCls(loc.pathname.startsWith('/pricing/pricelists'))}>لیست قیمت‌ها</Link>
            <Link to="/pricing/overrides" className={navCls(loc.pathname.startsWith('/pricing/overrides'))}>قیمت‌های ویژه</Link>
            <Link to="/pricing/campaigns" className={navCls(loc.pathname.startsWith('/pricing/campaigns'))}>کمپین‌ها</Link>
            <Link to="/pricing/coupons" className={navCls(loc.pathname.startsWith('/pricing/coupons'))}>کدهای تخفیف</Link>
            <Link to="/pricing/bulk" className={navCls(loc.pathname.startsWith('/pricing/bulk'))}>عملیات گروهی</Link>
            <Link to="/pricing/quote" className={navCls(loc.pathname.startsWith('/pricing/quote'))}>پیش‌نمایش قیمت</Link>
            <Link to="/pricing/product" className={navCls(loc.pathname.startsWith('/pricing/product'))}>قیمت‌گذاری محصول</Link>
            <Link to="/pricing/report" className={navCls(loc.pathname.startsWith('/pricing/report'))}>گزارش قیمت‌ها</Link>
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
