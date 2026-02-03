import { Link, Outlet, useLocation } from 'react-router-dom'
import { LoginPage } from '../../app/LoginPage'
import { useAdminAuth } from '../../app/auth'

export function Layout() {
  const loc = useLocation()
  const { accessToken } = useAdminAuth()

  if (!accessToken) {
    return <LoginPage />
  }

  return (
    <div className="min-h-screen flex flex-col">
      <header className="bg-white border-b">
        <div className="container-std flex items-center justify-between h-14">
          <Link to="/" className="font-semibold">مدیریت هویت</Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/identity/auth" className={navCls(loc.pathname.startsWith('/identity/auth'))}>احراز هویت</Link>
            <Link to="/identity/me" className={navCls(loc.pathname.startsWith('/identity/me'))}>پروفایل</Link>
            <Link to="/identity/users" className={navCls(loc.pathname.startsWith('/identity/users'))}>کاربران</Link>
            <Link to="/identity/reports" className={navCls(loc.pathname.startsWith('/identity/reports'))}>گزارش‌ها</Link>
            <Link to="/identity/tokens" className={navCls(loc.pathname.startsWith('/identity/tokens'))}>توکن‌ها</Link>
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
