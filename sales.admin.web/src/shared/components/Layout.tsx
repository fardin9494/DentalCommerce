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
          <Link to="/" className="font-semibold">مدیریت فروش</Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/sales/orders" className={navCls(loc.pathname.startsWith('/sales/orders'))}>سفارش‌ها</Link>
            <Link to="/sales/reports" className={navCls(loc.pathname.startsWith('/sales/reports'))}>گزارش‌ها</Link>
            <Link to="/sales/test" className={navCls(loc.pathname.startsWith('/sales/test'))}>تست سناریو خرید</Link>
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
