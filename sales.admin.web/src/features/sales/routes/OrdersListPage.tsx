import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { fetchJson, fetchJsonWithBase } from '@/lib/api/client'
import { CATALOG_API_BASE } from '@/app/env'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { formatDate, formatNumber } from '@/shared/utils/date'

type OrderListItem = {
  id: string
  siteId: string
  userId?: string | null
  orderNumber: string
  siteName?: string | null
  siteDomain?: string | null
  pricingQuoteId: string
  currency: string
  status: string
  createdAt: string
  updatedAt: string
  placedAtUtc?: string | null
  paymentFailedAtUtc?: string | null
  paymentFailureReason?: string | null
  subtotal: number
  discountTotal: number
  finalTotal: number
  cashbackTotal: number
  linesCount: number
}

type OrderListResult = {
  items: OrderListItem[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

type StoreOption = { id: string; name: string; domain?: string | null }

type Filters = {
  search: string
  siteId: string
  status: string
  page: number
  pageSize: number
}

const emptyFilters: Filters = {
  search: '',
  siteId: '',
  status: '',
  page: 1,
  pageSize: 20,
}

export function OrdersListPage() {
  const toast = useToast()
  const navigate = useNavigate()
  const [form, setForm] = useState<Filters>({ ...emptyFilters })
  const [query, setQuery] = useState<Filters>({ ...emptyFilters })
  const [data, setData] = useState<OrderListResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [storeSearch, setStoreSearch] = useState('')
  const [storeOptions, setStoreOptions] = useState<StoreOption[]>([])
  const [storeLoading, setStoreLoading] = useState(false)
  const storeTimer = useRef<number | undefined>(undefined)

  const qs = useMemo(() => {
    const params = new URLSearchParams()
    if (query.search.trim()) params.set('search', query.search.trim())
    if (query.siteId.trim()) params.set('siteId', query.siteId.trim())
    if (query.status) params.set('status', query.status)
    params.set('page', String(query.page))
    params.set('pageSize', String(query.pageSize))
    const s = params.toString()
    return s ? `/sales/orders?${s}` : '/sales/orders'
  }, [query])

  useEffect(() => {
    let ignore = false
    async function load() {
      setLoading(true)
      setError(null)
      try {
        const res = await fetchJson<OrderListResult>(qs)
        if (!ignore) setData(res)
      } catch (err: any) {
        if (!ignore) {
          const msg = err?.message || 'خطا در دریافت سفارش‌ها'
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

  useEffect(() => {
    const q = storeSearch.trim()
    if (storeTimer.current) window.clearTimeout(storeTimer.current)
    if (q.length < 2) {
      setStoreOptions([])
      return
    }
    storeTimer.current = window.setTimeout(async () => {
      setStoreLoading(true)
      try {
        const res = await fetchJsonWithBase<StoreOption[]>(CATALOG_API_BASE, `/stores?search=${encodeURIComponent(q)}`)
        setStoreOptions(res || [])
      } catch {
        setStoreOptions([])
      } finally {
        setStoreLoading(false)
      }
    }, 300)
    return () => {
      if (storeTimer.current) window.clearTimeout(storeTimer.current)
    }
  }, [storeSearch])

  const applyFilters = () => setQuery({ ...form, page: 1 })
  const resetFilters = () => {
    setForm({ ...emptyFilters })
    setQuery({ ...emptyFilters })
    setStoreSearch('')
    setStoreOptions([])
  }

  const goToPage = (page: number) => setQuery(prev => ({ ...prev, page }))

  return (
    <div className="space-y-6">
      <div className="card p-4">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">لیست سفارش‌ها</h2>
            <p className="text-sm text-gray-500">آخرین وضعیت سفارش‌ها و جزئیات پرداخت</p>
          </div>
          <div className="flex items-center gap-2">
            <button className="btn-secondary" onClick={applyFilters} disabled={loading}>جستجو</button>
            <button className="btn-secondary" onClick={resetFilters} disabled={loading}>ریست</button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3 mt-4">
          <div>
            <label className="label">جستجو (OrderId / QuoteId / UserId)</label>
            <input className="input" value={form.search} onChange={e => setForm(f => ({ ...f, search: e.target.value }))} placeholder="GUID" />
          </div>
          <div className="relative">
            <label className="label">سایت (انتخاب)</label>
            <input
              className="input"
              value={storeSearch}
              onChange={e => {
                const v = e.target.value
                setStoreSearch(v)
                if (!v.trim()) setForm(f => ({ ...f, siteId: '' }))
              }}
              placeholder="جستجوی نام یا دامنه"
            />
            {storeLoading && <div className="text-xs text-gray-500 mt-1">در حال جستجو...</div>}
            {storeOptions.length > 0 && (
              <div className="absolute z-20 mt-1 w-full bg-white border rounded shadow max-h-60 overflow-auto">
                {storeOptions.map(s => (
                  <button
                    key={s.id}
                    type="button"
                    className="block w-full text-right px-3 py-2 text-sm hover:bg-gray-50"
                    onClick={() => {
                      setForm(f => ({ ...f, siteId: s.id }))
                      setStoreSearch(s.domain ? `${s.name} (${s.domain})` : s.name)
                      setStoreOptions([])
                    }}
                  >
                    <div className="font-medium">{s.name}</div>
                    <div className="text-xs text-gray-500">{s.domain || s.id}</div>
                  </button>
                ))}
              </div>
            )}
            <div className="text-xs text-gray-500 mt-1">SiteId: {form.siteId || '—'}</div>
          </div>
          <div>
            <label className="label">وضعیت</label>
            <select className="input" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value }))}>
              <option value="">همه</option>
              <option value="Draft">Draft</option>
              <option value="Placed">Placed</option>
              <option value="Shipped">Shipped</option>
              <option value="Delivered">Delivered</option>
              <option value="Returned">Returned</option>
              <option value="Refunded">Refunded</option>
              <option value="PaymentFailed">PaymentFailed</option>
              <option value="Cancelled">Cancelled</option>
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

      <div className="card p-4">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-3 mb-3">
          <div className="text-sm text-gray-600">
            {loading ? 'در حال دریافت...' : `تعداد کل: ${formatNumber(data?.totalCount)} | صفحه: ${data?.page ?? 1} از ${data?.totalPages ?? 1}`}
          </div>
          <div className="flex items-center gap-2">
            <button className="btn-secondary" onClick={() => goToPage(Math.max(1, (data?.page ?? 1) - 1))} disabled={loading || (data?.page ?? 1) <= 1}>قبلی</button>
            <button className="btn-secondary" onClick={() => goToPage(Math.min(data?.totalPages ?? 1, (data?.page ?? 1) + 1))} disabled={loading || (data?.page ?? 1) >= (data?.totalPages ?? 1)}>بعدی</button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full text-sm text-center">
            <thead>
              <tr className="bg-gray-100">
                <th className="p-2">سفارش</th>
                <th className="p-2">وضعیت</th>
                <th className="p-2">Site</th>
                <th className="p-2">مبلغ نهایی</th>
                <th className="p-2">تاریخ ثبت</th>
                <th className="p-2">خطا</th>
                <th className="p-2">عملیات</th>
              </tr>
            </thead>
            <tbody>
              {(!data?.items || data.items.length === 0) && (
                <tr className="border-b last:border-0">
                  <td colSpan={7} className="p-3 text-sm text-gray-600">سفارشی یافت نشد.</td>
                </tr>
              )}
              {data?.items?.map(order => (
                <tr key={order.id} className="border-b last:border-0">
                  <td className="p-2">
                    <div className="text-sm font-medium">{order.orderNumber || '—'}</div>
                  </td>
                  <td className="p-2">
                    <span className={statusBadge(order.status)}>{order.status}</span>
                  </td>
                  <td className="p-2">
                    <div className="text-sm">{order.siteName || '—'}</div>
                    {order.siteDomain && (
                      <div className="text-[11px] text-gray-500">{order.siteDomain}</div>
                    )}
                  </td>
                  <td className="p-2">{formatNumber(order.finalTotal)} {order.currency}</td>
                  <td className="p-2">{formatDate(order.createdAt)}</td>
                  <td className="p-2">
                    {order.paymentFailureReason ? (
                      <span className="text-xs text-red-600">{order.paymentFailureReason}</span>
                    ) : (
                      <span className="text-xs text-gray-400">—</span>
                    )}
                  </td>
                  <td className="p-2">
                    <button className="btn-secondary px-3 py-1.5" onClick={() => navigate(`/sales/orders/${order.id}`)}>جزئیات</button>
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

function statusBadge(status: string) {
  const s = status?.toLowerCase()
  if (s === 'placed') return 'badge badge-green'
  if (s === 'shipped') return 'badge badge-blue'
  if (s === 'delivered') return 'badge badge-green'
  if (s === 'returned') return 'badge badge-amber'
  if (s === 'refunded') return 'badge badge-amber'
  if (s === 'paymentfailed') return 'badge badge-red'
  if (s === 'cancelled') return 'badge badge-gray'
  return 'badge badge-gray'
}
