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
          <Link to="/" className="font-semibold">Admin.Web</Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/products" className={navCls(loc.pathname.startsWith('/products'))}>محصولات</Link>
            <Link to="/brands" className={navCls(loc.pathname.startsWith('/brands'))}>برندها</Link>
            <Link to="/categories" className={navCls(loc.pathname.startsWith('/categories'))}>دسته‌ها</Link>
            <Link to="/stores" className={navCls(loc.pathname.startsWith('/stores'))}>فروشگاه ها</Link>
            <Link to="/countries" className={navCls(loc.pathname.startsWith('/countries'))}>کشور ها</Link>
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
