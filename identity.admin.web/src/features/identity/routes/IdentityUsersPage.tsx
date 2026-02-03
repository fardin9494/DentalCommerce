import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchJson } from '@/lib/api/client'
import { useToast } from '@/shared/components/toast/ToastProvider'

type AdminUserSummary = {
  userId: string
  phoneNumber: string
  fullName: string | null
  createdAtUtc: string
  lastLoginAtUtc: string | null
  isBanned: boolean
  banUntilUtc: string | null
  banReason: string | null
  lockedUntilUtc: string | null
  failedCount: number
  hasPassword: boolean
}

type AdminUsersPage = {
  totalCount: number
  items: AdminUserSummary[]
}

type Filters = {
  q: string
  status: string
  page: number
  pageSize: number
}

const emptyFilters: Filters = {
  q: '',
  status: 'all',
  page: 1,
  pageSize: 20,
}

export function IdentityUsersPage() {
  const toast = useToast()
  const navigate = useNavigate()
  const [form, setForm] = useState<Filters>({ ...emptyFilters })
  const [query, setQuery] = useState<Filters>({ ...emptyFilters })
  const [data, setData] = useState<AdminUsersPage | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const qs = useMemo(() => {
    const params = new URLSearchParams()
    if (query.q.trim()) params.set('q', query.q.trim())
    if (query.status && query.status !== 'all') params.set('status', query.status)
    params.set('page', String(query.page))
    params.set('pageSize', String(query.pageSize))
    return `/admin/users?${params.toString()}`
  }, [query])

  useEffect(() => {
    let ignore = false
    async function load() {
      setLoading(true)
      setError(null)
      try {
        const res = await fetchJson<AdminUsersPage>(qs)
        if (!ignore) setData(res)
      } catch (err: any) {
        if (!ignore) {
          const msg = err?.message || 'خطا در دریافت کاربران'
          setError(msg)
          toast.error(msg)
        }
      } finally {
        if (!ignore) setLoading(false)
      }
    }
    load()
    return () => { ignore = true }
  }, [qs, toast])

  const applyFilters = () => setQuery({ ...form, page: 1 })
  const resetFilters = () => {
    setForm({ ...emptyFilters })
    setQuery({ ...emptyFilters })
  }
  const goToPage = (page: number) => setQuery(prev => ({ ...prev, page }))

  async function banUser(u: AdminUserSummary) {
    const reason = window.prompt('علت بن (اختیاری):') || ''
    const minutesRaw = window.prompt('مدت بن (دقیقه) - خالی برای دائمی:')
    const minutes = minutesRaw ? Number(minutesRaw) : NaN
    const untilUtc = Number.isFinite(minutes) ? new Date(Date.now() + minutes * 60000).toISOString() : null
    try {
      await fetchJson<void>(`/admin/users/${u.userId}/ban`, {
        method: 'POST',
        json: { reason, untilUtc },
      })
      toast.success('کاربر بن شد')
      applyFilters()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در بن کردن')
    }
  }

  async function unbanUser(u: AdminUserSummary) {
    try {
      await fetchJson<void>(`/admin/users/${u.userId}/unban`, { method: 'POST' })
      toast.success('بن برداشته شد')
      applyFilters()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در آن‌بن کردن')
    }
  }

  async function unlockUser(u: AdminUserSummary) {
    try {
      await fetchJson<void>(`/admin/users/${u.userId}/unlock`, { method: 'POST' })
      toast.success('قفل تلاش‌ها پاک شد')
      applyFilters()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در آنلاک')
    }
  }

  return (
    <div className="space-y-6" dir="rtl">
      <div className="card">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">مدیریت کاربران</h2>
            <p className="text-sm text-gray-500">جستجو، وضعیت بن/لاک و کنترل سریع</p>
          </div>
          <div className="flex items-center gap-2">
            <button className="btn-ghost" onClick={applyFilters} disabled={loading}>جستجو</button>
            <button className="btn-ghost" onClick={resetFilters} disabled={loading}>ریست</button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3 mt-4">
          <div>
            <label className="label">جستجو (موبایل / نام)</label>
            <input className="input" value={form.q} onChange={e => setForm(f => ({ ...f, q: e.target.value }))} placeholder="0912..." />
          </div>
          <div>
            <label className="label">وضعیت</label>
            <select className="input" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value }))}>
              <option value="all">همه</option>
              <option value="active">فعال</option>
              <option value="banned">بن شده</option>
              <option value="locked">لاک شده</option>
            </select>
          </div>
          <div>
            <label className="label">تعداد در صفحه</label>
            <select className="input" value={form.pageSize} onChange={e => setForm(f => ({ ...f, pageSize: Number(e.target.value) }))}>
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
            </select>
          </div>
        </div>

        {error && <div className="mt-3 text-sm text-red-600">{error}</div>}
      </div>

      <div className="card">
        <div className="flex items-center justify-between mb-3 text-sm text-gray-600">
          <div>تعداد کل: {data?.totalCount ?? 0}</div>
          <div className="flex gap-2">
            <button className="btn-ghost" onClick={() => goToPage(Math.max(1, (query.page ?? 1) - 1))} disabled={loading || (query.page ?? 1) <= 1}>قبلی</button>
            <button className="btn-ghost" onClick={() => goToPage((query.page ?? 1) + 1)} disabled={loading}>بعدی</button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full text-sm text-center">
            <thead>
              <tr className="bg-gray-100">
                <th className="p-2">موبایل</th>
                <th className="p-2">نام</th>
                <th className="p-2">وضعیت</th>
                <th className="p-2">تاریخ ایجاد</th>
                <th className="p-2">آخرین ورود</th>
                <th className="p-2">عملیات</th>
              </tr>
            </thead>
            <tbody>
              {(!data?.items || data.items.length === 0) && (
                <tr className="border-b last:border-0">
                  <td colSpan={6} className="p-3 text-sm text-gray-600">کاربری یافت نشد.</td>
                </tr>
              )}
              {data?.items?.map(u => (
                <tr key={u.userId} className="border-b last:border-0">
                  <td className="p-2">{u.phoneNumber}</td>
                  <td className="p-2">{u.fullName || '—'}</td>
                  <td className="p-2">
                    {u.isBanned ? (
                      <span className="badge badge-red">Ban</span>
                    ) : u.lockedUntilUtc ? (
                      <span className="badge badge-amber">Locked</span>
                    ) : (
                      <span className="badge badge-green">Active</span>
                    )}
                  </td>
                  <td className="p-2">{formatDate(u.createdAtUtc)}</td>
                  <td className="p-2">{formatDate(u.lastLoginAtUtc)}</td>
                  <td className="p-2">
                    <div className="flex items-center justify-center gap-2">
                      <button className="btn-ghost" onClick={() => navigate(`/identity/users/${u.userId}`)}>جزئیات</button>
                      {u.isBanned ? (
                        <button className="btn-ghost" onClick={() => unbanUser(u)}>آن‌بن</button>
                      ) : (
                        <button className="btn-ghost" onClick={() => banUser(u)}>بن</button>
                      )}
                      <button className="btn-ghost" onClick={() => unlockUser(u)}>آنلاک</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}

function formatDate(value?: string | null) {
  if (!value) return '—'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('fa-IR')
}
