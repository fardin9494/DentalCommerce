import { useEffect, useMemo, useState } from 'react'
import { fetchJson, fetchJsonWithBase } from '@/lib/api/client'
import { CATALOG_API_BASE } from '@/app/env'
import { formatDate, formatNumber } from '@/shared/utils/date'
import { useToast } from '@/shared/components/toast/ToastProvider'

type StoreOption = { id: string; name: string; domain?: string | null }

type DailyItem = {
  date: string
  ordersCount: number
  itemsCount: number
  subtotal: number
  discountTotal: number
  finalTotal: number
  cashbackTotal: number
}

type MonthlyItem = {
  year: number
  month: number
  ordersCount: number
  itemsCount: number
  subtotal: number
  discountTotal: number
  finalTotal: number
  cashbackTotal: number
}

type SiteItem = {
  siteId: string
  siteName?: string | null
  siteDomain?: string | null
  ordersCount: number
  itemsCount: number
  finalTotal: number
}

type ProductItem = {
  skuId: string
  ordersCount: number
  quantity: number
  revenue: number
}

type CustomerItem = {
  userId?: string | null
  ordersCount: number
  finalTotal: number
}

export function SalesReportsPage() {
  const toast = useToast()
  const [fromDate, setFromDate] = useState('')
  const [toDate, setToDate] = useState('')
  const [siteId, setSiteId] = useState('')
  const [statusFilter, setStatusFilter] = useState('all')
  const [allStores, setAllStores] = useState<StoreOption[]>([])
  const [storesLoading, setStoresLoading] = useState(false)

  const [daily, setDaily] = useState<DailyItem[]>([])
  const [monthly, setMonthly] = useState<MonthlyItem[]>([])
  const [bySite, setBySite] = useState<SiteItem[]>([])
  const [byProduct, setByProduct] = useState<ProductItem[]>([])
  const [byCustomer, setByCustomer] = useState<CustomerItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [hasLoadedOnce, setHasLoadedOnce] = useState(false)

  const queryString = useMemo(() => {
    const base = new URLSearchParams()
    if (fromDate) base.set('fromUtc', new Date(`${fromDate}T00:00:00Z`).toISOString())
    if (toDate) base.set('toUtc', new Date(`${toDate}T23:59:59Z`).toISOString())
    // Always send status parameter - backend handles 'all' correctly
    if (statusFilter) base.set('status', statusFilter)

    const withSite = new URLSearchParams(base)
    if (siteId) withSite.set('siteId', siteId)

    const withSiteStr = withSite.toString()
    const noSiteStr = base.toString()

    return {
      withSite: withSiteStr ? `?${withSiteStr}` : '',
      noSite: noSiteStr ? `?${noSiteStr}` : ''
    }
  }, [fromDate, toDate, siteId, statusFilter])

  const loadReports = async () => {
    setLoading(true)
    setError(null)
    try {
      const [d, m, s, p, c] = await Promise.all([
        fetchJson<DailyItem[]>(`/sales/reports/daily${queryString.withSite}`),
        fetchJson<MonthlyItem[]>(`/sales/reports/monthly${queryString.withSite}`),
        fetchJson<SiteItem[]>(`/sales/reports/by-site${queryString.noSite}`),
        fetchJson<ProductItem[]>(`/sales/reports/by-product${queryString.withSite}`),
        fetchJson<CustomerItem[]>(`/sales/reports/by-customer${queryString.withSite}`),
      ])
      setDaily(d || [])
      setMonthly(m || [])
      setBySite(s || [])
      setByProduct(p || [])
      setByCustomer(c || [])
      setHasLoadedOnce(true)
    } catch (err: any) {
      const msg = err?.message || 'خطا در دریافت گزارش‌ها'
      setError(msg)
      toast.error(msg)
    } finally {
      setLoading(false)
    }
  }

  // Auto-refresh when status filter changes if reports were already loaded
  useEffect(() => {
    if (hasLoadedOnce && !loading) {
      loadReports()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter])

  // Load all stores on component mount
  useEffect(() => {
    const loadStores = async () => {
      setStoresLoading(true)
      try {
        const res = await fetchJsonWithBase<StoreOption[]>(CATALOG_API_BASE, `/stores`)
        setAllStores(res || [])
      } catch (err) {
        console.error('Failed to load stores:', err)
        setAllStores([])
      } finally {
        setStoresLoading(false)
      }
    }
    loadStores()
  }, [])

  // Calculate summary statistics
  const summary = useMemo(() => {
    const totalOrders = daily.reduce((sum, d) => sum + d.ordersCount, 0)
    const totalItems = daily.reduce((sum, d) => sum + d.itemsCount, 0)
    const totalRevenue = daily.reduce((sum, d) => sum + d.finalTotal, 0)
    const totalDiscount = daily.reduce((sum, d) => sum + d.discountTotal, 0)
    return { totalOrders, totalItems, totalRevenue, totalDiscount }
  }, [daily])

  return (
    <div className="space-y-6">
      {/* Header Card */}
      <div className="card p-6 bg-gradient-to-br from-emerald-50 to-blue-50 border-emerald-200">
        <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900 mb-1">گزارش‌های فروش</h1>
            <p className="text-sm text-gray-600">تحلیل جامع فروش به تفکیک روز، ماه، سایت، محصول و مشتری</p>
          </div>
          <button 
            className="btn flex items-center gap-2 shadow-md hover:shadow-lg transition-shadow" 
            onClick={loadReports} 
            disabled={loading}
          >
            {loading ? (
              <>
                <svg className="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                <span>در حال دریافت...</span>
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
                <span>دریافت گزارش</span>
              </>
            )}
          </button>
        </div>

        {/* Summary Cards */}
        {hasLoadedOnce && (
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mt-6">
            <div className="bg-white/80 backdrop-blur-sm rounded-lg p-4 border border-emerald-200 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-gray-600 mb-1">کل سفارش‌ها</p>
                  <p className="text-2xl font-bold text-gray-900">{formatNumber(summary.totalOrders)}</p>
                </div>
                <div className="w-12 h-12 bg-emerald-100 rounded-full flex items-center justify-center">
                  <svg className="w-6 h-6 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white/80 backdrop-blur-sm rounded-lg p-4 border border-blue-200 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-gray-600 mb-1">کل آیتم‌ها</p>
                  <p className="text-2xl font-bold text-gray-900">{formatNumber(summary.totalItems)}</p>
                </div>
                <div className="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
                  <svg className="w-6 h-6 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white/80 backdrop-blur-sm rounded-lg p-4 border border-purple-200 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-gray-600 mb-1">کل درآمد</p>
                  <p className="text-2xl font-bold text-gray-900">{formatNumber(summary.totalRevenue)}</p>
                </div>
                <div className="w-12 h-12 bg-purple-100 rounded-full flex items-center justify-center">
                  <svg className="w-6 h-6 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
              </div>
            </div>
            <div className="bg-white/80 backdrop-blur-sm rounded-lg p-4 border border-amber-200 shadow-sm">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-xs text-gray-600 mb-1">کل تخفیف</p>
                  <p className="text-2xl font-bold text-gray-900">{formatNumber(summary.totalDiscount)}</p>
                </div>
                <div className="w-12 h-12 bg-amber-100 rounded-full flex items-center justify-center">
                  <svg className="w-6 h-6 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Filters Card */}
      <div className="card p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4 flex items-center gap-2">
          <svg className="w-5 h-5 text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" />
          </svg>
          فیلترها
        </h3>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="label flex items-center gap-2">
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              از تاریخ
            </label>
            <input className="input" type="date" value={fromDate} onChange={e => setFromDate(e.target.value)} />
          </div>
          <div>
            <label className="label flex items-center gap-2">
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              تا تاریخ
            </label>
            <input className="input" type="date" value={toDate} onChange={e => setToDate(e.target.value)} />
          </div>
          <div>
            <label className="label flex items-center gap-2">
              <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
              سایت (اختیاری)
            </label>
            <select
              className="input"
              value={siteId}
              onChange={e => setSiteId(e.target.value)}
              disabled={storesLoading}
            >
              <option value="">همه سایت‌ها</option>
              {allStores.map(store => (
                <option key={store.id} value={store.id}>
                  {store.name}{store.domain ? ` (${store.domain})` : ''}
                </option>
              ))}
            </select>
            {storesLoading && (
              <div className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                <svg className="animate-spin h-3 w-3" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                </svg>
                در حال بارگذاری سایت‌ها...
              </div>
            )}
          </div>
          <div>
            <label className="label">عملیات</label>
            <button 
              className="btn-secondary w-full flex items-center justify-center gap-2" 
              onClick={() => {
                setFromDate('')
                setToDate('')
                setSiteId('')
                setStatusFilter('all')
              }}
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
              پاکسازی فیلترها
            </button>
          </div>
        </div>

        <div className="mt-6">
          <label className="label flex items-center gap-2 mb-3">
            <svg className="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            فیلتر وضعیت سفارش
            {hasLoadedOnce && (
              <span className="text-xs text-gray-500 font-normal">(تغییر وضعیت به صورت خودکار گزارش را به‌روز می‌کند)</span>
            )}
          </label>
          <div className="flex flex-wrap gap-2">
            {statusOptions.map(opt => (
              <button
                key={opt.value}
                type="button"
                className={`${badgeClass(opt, statusFilter === opt.value)} cursor-pointer transition-all duration-200 ${
                  statusFilter === opt.value 
                    ? 'ring-2 ring-offset-2 ring-emerald-500 scale-105 shadow-md' 
                    : 'hover:scale-105 hover:shadow-sm'
                }`}
                onClick={() => setStatusFilter(opt.value)}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </div>

        {error && (
          <div className="mt-4 p-3 bg-red-50 border border-red-200 rounded-lg text-sm text-red-700 flex items-center gap-2">
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            {error}
          </div>
        )}
      </div>

      <ReportTable
        title="گزارش روزانه"
        headers={['تاریخ', 'تعداد سفارش', 'تعداد آیتم', 'جمع نهایی']}
        rows={daily.map(d => [
          formatDate(d.date),
          formatNumber(d.ordersCount),
          formatNumber(d.itemsCount),
          formatNumber(d.finalTotal),
        ])}
      />

      <ReportTable
        title="گزارش ماهانه"
        headers={['ماه', 'تعداد سفارش', 'تعداد آیتم', 'جمع نهایی']}
        rows={monthly.map(m => [
          `${m.year}-${String(m.month).padStart(2, '0')}`,
          formatNumber(m.ordersCount),
          formatNumber(m.itemsCount),
          formatNumber(m.finalTotal),
        ])}
      />

      <ReportTable
        title="فروش به تفکیک سایت"
        headers={['سایت', 'تعداد سفارش', 'تعداد آیتم', 'جمع نهایی']}
        rows={bySite.map(s => [
          s.siteDomain ? `${s.siteName} (${s.siteDomain})` : (s.siteName || s.siteId),
          formatNumber(s.ordersCount),
          formatNumber(s.itemsCount),
          formatNumber(s.finalTotal),
        ])}
      />

      <ReportTable
        title="فروش به تفکیک محصول (SKU)"
        headers={['SKU', 'تعداد سفارش', 'تعداد', 'درآمد']}
        rows={byProduct.map(p => [
          p.skuId,
          formatNumber(p.ordersCount),
          formatNumber(p.quantity),
          formatNumber(p.revenue),
        ])}
      />

      <ReportTable
        title="فروش به تفکیک مشتری"
        headers={['مشتری', 'تعداد سفارش', 'جمع نهایی']}
        rows={byCustomer.map(c => [
          c.userId || 'Anonymous',
          formatNumber(c.ordersCount),
          formatNumber(c.finalTotal),
        ])}
      />
    </div>
  )
}

function ReportTable({ title, headers, rows }: { title: string; headers: string[]; rows: string[][] }) {
  return (
    <div className="card p-6 hover:shadow-lg transition-shadow duration-200">
      <h3 className="font-semibold text-lg text-gray-900 mb-4 flex items-center gap-2">
        <svg className="w-5 h-5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
        {title}
        <span className="text-sm font-normal text-gray-500">({rows.length} مورد)</span>
      </h3>
      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="min-w-full text-sm">
          <thead>
            <tr className="bg-gradient-to-r from-gray-50 to-gray-100">
              {headers.map(h => (
                <th key={h} className="px-4 py-3 text-right font-semibold text-gray-700 border-b border-gray-200">
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {rows.length === 0 && (
              <tr>
                <td colSpan={headers.length} className="px-4 py-8 text-center text-gray-500">
                  <div className="flex flex-col items-center gap-2">
                    <svg className="w-12 h-12 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                    </svg>
                    <span>داده‌ای ثبت نشده است</span>
                  </div>
                </td>
              </tr>
            )}
            {rows.map((row, i) => (
              <tr 
                key={i} 
                className="hover:bg-emerald-50/50 transition-colors duration-150 cursor-pointer"
              >
                {row.map((cell, idx) => (
                  <td key={idx} className="px-4 py-3 text-right text-gray-700">
                    {cell}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

const statusOptions = [
  { value: 'all', label: 'همه', tone: 'gray' },
  { value: 'Draft', label: 'Draft', tone: 'gray' },
  { value: 'Placed', label: 'Placed', tone: 'green' },
  { value: 'Shipped', label: 'Shipped', tone: 'blue' },
  { value: 'Delivered', label: 'Delivered', tone: 'green' },
  { value: 'Returned', label: 'Returned', tone: 'amber' },
  { value: 'Refunded', label: 'Refunded', tone: 'amber' },
  { value: 'PaymentFailed', label: 'PaymentFailed', tone: 'red' },
  { value: 'Cancelled', label: 'Cancelled', tone: 'gray' },
]

function badgeClass(opt: { tone: string }, active: boolean) {
  const base = 'badge px-3 py-1.5 font-medium'
  const tone = opt.tone
  const toneClass = tone === 'green'
    ? 'bg-green-100 text-green-800 border border-green-300'
    : tone === 'red'
      ? 'bg-red-100 text-red-800 border border-red-300'
      : tone === 'blue'
        ? 'bg-blue-100 text-blue-800 border border-blue-300'
        : tone === 'amber'
          ? 'bg-amber-100 text-amber-800 border border-amber-300'
          : 'bg-gray-100 text-gray-800 border border-gray-300'
  return `${base} ${toneClass} ${active ? 'ring-2 ring-offset-1 ring-emerald-400 font-bold' : 'opacity-70 hover:opacity-100'}`
}
