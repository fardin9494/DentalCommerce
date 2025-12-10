import { useState, useMemo } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useStockLedger } from '../queries'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { StockMovementTypeLabels, StockMovementTypeColors, type StockLedgerFilters } from '../types'
import { StockLedgerEntryDetailsModal } from '../components/StockLedgerEntryDetailsModal'
import DatePicker from 'react-multi-date-picker'
import DateObject from 'react-date-object'
import persian from 'react-date-object/calendars/persian'
import persian_fa from 'react-date-object/locales/persian_fa'

export function StockLedgerPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()

  // Get filters from URL
  const filters: StockLedgerFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '50', 10),
      warehouseId: searchParams.get('warehouseId') || undefined,
      productId: searchParams.get('productId') || undefined,
      variantId: searchParams.get('variantId') || undefined,
      movementType: searchParams.get('movementType') ? parseInt(searchParams.get('movementType')!, 10) : undefined,
      refDocType: searchParams.get('refDocType') || undefined,
      refDocId: searchParams.get('refDocId') || undefined,
      fromDate: searchParams.get('fromDate') || undefined,
      toDate: searchParams.get('toDate') || undefined,
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useStockLedger(filters)

  // Local filter states
  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')
  const [movementTypeFilter, setMovementTypeFilter] = useState<string>(filters.movementType?.toString() || '')
  const [refDocTypeFilter, setRefDocTypeFilter] = useState<string>(filters.refDocType || '')
  const [fromDateFilter, setFromDateFilter] = useState<DateObject | null>(
    filters.fromDate ? new DateObject({ date: new Date(filters.fromDate), calendar: persian, locale: persian_fa }) : null
  )
  const [toDateFilter, setToDateFilter] = useState<DateObject | null>(
    filters.toDate ? new DateObject({ date: new Date(filters.toDate), calendar: persian, locale: persian_fa }) : null
  )
  const [selectedEntryId, setSelectedEntryId] = useState<string | null>(null)

  function updateFilters(newFilters: Partial<StockLedgerFilters>) {
    const params = new URLSearchParams(searchParams)

    Object.entries(newFilters).forEach(([key, value]) => {
      if (value === undefined || value === '' || value === null) {
        params.delete(key)
      } else {
        params.set(key, String(value))
      }
    })

    if (!('page' in newFilters)) {
      params.set('page', '1')
    }

    setSearchParams(params)
  }

  function handleWarehouseChange(value: string) {
    setWarehouseFilter(value)
    updateFilters({ warehouseId: value || undefined })
  }

  function handleMovementTypeChange(value: string) {
    setMovementTypeFilter(value)
    updateFilters({ movementType: value ? parseInt(value, 10) : undefined })
  }

  function handleRefDocTypeChange(value: string) {
    setRefDocTypeFilter(value)
    updateFilters({ refDocType: value || undefined })
  }

  function handleFromDateChange(date: DateObject | DateObject[] | null) {
    const d = Array.isArray(date) ? (date[0] as DateObject | null) : (date as DateObject | null)
    setFromDateFilter(d)
    updateFilters({ fromDate: d ? d.toDate().toISOString() : undefined })
  }

  function handleToDateChange(date: DateObject | DateObject[] | null) {
    const d = Array.isArray(date) ? (date[0] as DateObject | null) : (date as DateObject | null)
    setToDateFilter(d)
    updateFilters({ toDate: d ? d.toDate().toISOString() : undefined })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setWarehouseFilter('')
    setMovementTypeFilter('')
    setRefDocTypeFilter('')
    setFromDateFilter(null)
    setToDateFilter(null)
    setSearchParams(new URLSearchParams())
  }

  function formatDateTime(dateStr: string) {
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  function formatDate(dateStr: string | null | undefined) {
    if (!dateStr) return '-'
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
      })
    } catch {
      return dateStr
    }
  }

  const hasActiveFilters = warehouseFilter || movementTypeFilter || refDocTypeFilter || fromDateFilter || toDateFilter

  const movementTypes = [
    { value: '1', label: 'ورود' },
    { value: '2', label: 'خروج' },
    { value: '3', label: 'انتقال (خروج)' },
    { value: '4', label: 'انتقال (ورود)' },
    { value: '5', label: 'اصلاح (افزایش)' },
    { value: '6', label: 'اصلاح (کاهش)' },
    { value: '7', label: 'انتقال قفسه (خروج)' },
    { value: '8', label: 'انتقال قفسه (ورود)' },
  ]

  const refDocTypes = ['Receipt', 'Issue', 'Transfer', 'Adjustment', 'StockMove']

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Spinner className="h-8 w-8 text-emerald-600" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="rounded-xl border border-red-200 bg-red-50 p-8 text-center">
        <p className="text-lg font-medium text-red-800">خطا در دریافت اطلاعات</p>
        <p className="mt-2 text-sm text-red-600">{(error as Error)?.message || 'خطای نامشخص'}</p>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader title="کاردکس انبار">مشاهده و بررسی تمام عملیات‌های انبار</PageHeader>

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="mb-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-900">فیلترها</h2>
          {hasActiveFilters && (
            <button
              onClick={clearFilters}
              className="text-sm font-medium text-slate-600 hover:text-slate-900"
            >
              پاک کردن فیلترها
            </button>
          )}
        </div>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">انبار</label>
            <select
              value={warehouseFilter}
              onChange={(e) => handleWarehouseChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه انبارها</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">نوع عملیات</label>
            <select
              value={movementTypeFilter}
              onChange={(e) => handleMovementTypeChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه انواع</option>
              {movementTypes.map((type) => (
                <option key={type.value} value={type.value}>
                  {type.label}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">نوع سند</label>
            <select
              value={refDocTypeFilter}
              onChange={(e) => handleRefDocTypeChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه انواع</option>
              {refDocTypes.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">از تاریخ (شمسی)</label>
            <DatePicker
              value={fromDateFilter}
              onChange={handleFromDateChange}
              calendar={persian}
              locale={persian_fa}
              calendarPosition="bottom-center"
              editable={false}
              portal
              inputClass="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              placeholder="انتخاب تاریخ"
              format="YYYY/MM/DD"
            />
          </div>
          <div>
            <label className="mb-1 block text-sm font-medium text-slate-700">تا تاریخ (شمسی)</label>
            <DatePicker
              value={toDateFilter}
              onChange={handleToDateChange}
              calendar={persian}
              locale={persian_fa}
              calendarPosition="bottom-center"
              editable={false}
              portal
              inputClass="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              placeholder="انتخاب تاریخ"
              format="YYYY/MM/DD"
            />
          </div>
        </div>
      </div>

      {/* Table */}
      {data && (
        <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="border-b border-slate-200 bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">تاریخ و زمان</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">نوع عملیات</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">مقدار</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">نوع سند</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">لات</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">تاریخ انقضا</th>
                  <th className="px-4 py-3 text-right text-sm font-semibold text-slate-700">یادداشت</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200">
                {data.items.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="px-4 py-8 text-center text-sm text-slate-500">
                      هیچ رکوردی یافت نشد
                    </td>
                  </tr>
                ) : (
                  data.items.map((entry) => (
                    <tr
                      key={entry.id}
                      className="cursor-pointer hover:bg-slate-50 transition-colors"
                      onClick={() => setSelectedEntryId(entry.id)}
                    >
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">
                        {formatDateTime(entry.timestamp)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span
                          className={`inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium ${
                            StockMovementTypeColors[entry.movementType]
                          }`}
                        >
                          {StockMovementTypeLabels[entry.movementType]}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <span
                          className={`font-semibold ${
                            entry.deltaQty > 0 ? 'text-emerald-600' : 'text-red-600'
                          }`}
                        >
                          {entry.deltaQty > 0 ? '+' : ''}
                          {entry.deltaQty.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">{entry.refDocType}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">{entry.lotNumber || '-'}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-600">
                        {formatDate(entry.expiryDate)}
                      </td>
                      <td className="px-4 py-3 text-sm text-slate-600">{entry.note || '-'}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data.totalPages > 1 && (
            <div className="border-t border-slate-200 bg-slate-50 px-4 py-3">
              <div className="flex items-center justify-between">
                <div className="text-sm text-slate-600">
                  نمایش {((data.page - 1) * data.pageSize + 1).toLocaleString('fa-IR')} تا{' '}
                  {Math.min(data.page * data.pageSize, data.totalCount).toLocaleString('fa-IR')} از{' '}
                  {data.totalCount.toLocaleString('fa-IR')} رکورد
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => handlePageChange(data.page - 1)}
                    disabled={data.page === 1}
                    className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
                  >
                    قبلی
                  </button>
                  <span className="text-sm text-slate-600">
                    صفحه {data.page.toLocaleString('fa-IR')} از {data.totalPages.toLocaleString('fa-IR')}
                  </span>
                  <button
                    onClick={() => handlePageChange(data.page + 1)}
                    disabled={data.page >= data.totalPages}
                    className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
                  >
                    بعدی
                  </button>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Details Modal */}
      <StockLedgerEntryDetailsModal entryId={selectedEntryId} onClose={() => setSelectedEntryId(null)} />
    </div>
  )
}

