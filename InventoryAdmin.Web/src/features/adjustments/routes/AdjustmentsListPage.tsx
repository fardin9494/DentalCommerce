import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import { useAdjustmentsList, useCreateAdjustment } from '../queries'
import { CreateAdjustmentModal } from '../components/CreateAdjustmentModal'
import {
  AdjustmentStatusLabels,
  AdjustmentReasonLabels,
  AdjustmentStatusColors,
  AdjustmentStatusMap,
  AdjustmentReasonMap,
  type AdjustmentStatus,
  type AdjustmentReason,
  type AdjustmentsListFilters,
} from '../types'
import { useActiveWarehouses, useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useSortableTable } from '@/shared/hooks/useSortableTable'
import { SortableHeader } from '@/shared/components/SortableHeader'
import { adjustmentDisplayNote } from '@/shared/utils/inventoryDocumentReference'
import { PermissionGate } from '@/app/permissions'
import { InventoryPermissionKeys } from '@/app/inventoryPermissionKeys'

// Type for adjustment list item
interface AdjustmentListItem {
  id: string
  warehouseId: string
  warehouseName?: string | null
  reason: string
  status: string
  docDate: string
  note?: string | null
  linesCount: number
  totalQtyDelta: number
  postedAt?: string | null
}

export function AdjustmentsListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()
  const { getWarehouseName } = useWarehouseNames()
  const createAdjustment = useCreateAdjustment()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [creating, setCreating] = useState(false)

  // Get filters from URL
  const filters: AdjustmentsListFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
      status: searchParams.get('status') ? parseInt(searchParams.get('status')!, 10) : undefined,
      reason: searchParams.get('reason') ? parseInt(searchParams.get('reason')!, 10) : undefined,
      search: searchParams.get('search') || undefined,
      warehouseId: searchParams.get('warehouseId') || undefined,
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useAdjustmentsList(filters)

  // Sorting - default by docDate descending (newest first)
  const { sortedData, requestSort, getSortIndicator, sortConfig } = useSortableTable<AdjustmentListItem>({
    data: (data?.items || []) as AdjustmentListItem[],
    defaultSortKey: 'docDate',
    defaultDirection: 'desc',
  })

  // Local filter states for UI
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [statusFilter, setStatusFilter] = useState<string>(filters.status?.toString() || '')
  const [reasonFilter, setReasonFilter] = useState<string>(filters.reason?.toString() || '')
  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')

  function updateFilters(newFilters: Partial<AdjustmentsListFilters>) {
    const params = new URLSearchParams(searchParams)

    Object.entries(newFilters).forEach(([key, value]) => {
      if (value === undefined || value === '' || value === null) {
        params.delete(key)
      } else {
        params.set(key, String(value))
      }
    })

    // Reset to page 1 when filters change (except when changing page)
    if (!('page' in newFilters)) {
      params.set('page', '1')
    }

    setSearchParams(params)
  }

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    updateFilters({ search: searchInput || undefined })
  }

  function handleStatusChange(value: string) {
    setStatusFilter(value)
    updateFilters({ status: value ? parseInt(value, 10) : undefined })
  }

  function handleReasonChange(value: string) {
    setReasonFilter(value)
    updateFilters({ reason: value ? parseInt(value, 10) : undefined })
  }

  function handleWarehouseChange(value: string) {
    setWarehouseFilter(value)
    updateFilters({ warehouseId: value || undefined })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setStatusFilter('')
    setReasonFilter('')
    setWarehouseFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleCreateAdjustment(data: { warehouseId: string; reason: number; note?: string }) {
    setCreating(true)
    try {
      const result = await createAdjustment.mutateAsync({
        warehouseId: data.warehouseId,
        reason: data.reason,
        note: data.note || null,
        docDateUtc: new Date().toISOString(),
      })
      setShowCreateModal(false)
      navigate(`/adjustments/${result.id}`)
    } catch {
      // Error handled by mutation
    } finally {
      setCreating(false)
    }
  }

  function formatDate(dateStr: string) {
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

  function formatDateTime(dateStr: string | null | undefined) {
    if (!dateStr) return '-'
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    } catch {
      return dateStr
    }
  }

  const hasActiveFilters = statusFilter || reasonFilter || searchInput || warehouseFilter

  return (
    <div className="space-y-6">
      <PageHeader
        title="انبارگردانی / اصلاحات"
        actions={
          <PermissionGate permission={InventoryPermissionKeys.AdjustmentsCreate}>
            <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            اصلاح جدید
          </button>
          </PermissionGate>
        }
      >
        مدیریت اصلاحات موجودی و انبارگردانی
      </PageHeader>

      {/* Create Adjustment Modal */}
      <CreateAdjustmentModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateAdjustment}
        isSubmitting={creating}
      />

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <form onSubmit={handleSearch} className="flex flex-wrap items-end gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">جستجو</label>
            <div className="relative">
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="یادداشت یا شناسه..."
                className="w-full rounded-lg border border-slate-300 py-2 pl-10 pr-4 text-sm placeholder-slate-400 focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
              />
              <svg
                className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400"
                fill="none"
                viewBox="0 0 24 24"
                strokeWidth={2}
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z"
                />
              </svg>
            </div>
          </div>

          {/* Warehouse Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">انبار</label>
            <select
              value={warehouseFilter}
              onChange={(e) => handleWarehouseChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">وضعیت</label>
            <select
              value={statusFilter}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه</option>
              <option value="1">پیش‌نویس</option>
              <option value="2">ثبت شده</option>
              <option value="3">لغو شده</option>
            </select>
          </div>

          {/* Reason Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">دلیل</label>
            <select
              value={reasonFilter}
              onChange={(e) => handleReasonChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه</option>
              <option value="1">موجودی اول دوره</option>
              <option value="2">خرابی/آسیب</option>
              <option value="3">تاریخ مصرف گذشته</option>
              <option value="4">یافت‌شده/اضافه</option>
              <option value="5">کسری/افت</option>
              <option value="6">اصلاح دستی</option>
              <option value="99">سایر</option>
            </select>
          </div>

          {/* Buttons */}
          <div className="flex gap-2">
            <button
              type="submit"
              className="rounded-lg bg-slate-100 px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-200"
            >
              اعمال
            </button>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="rounded-lg px-4 py-2 text-sm font-medium text-slate-500 transition-colors hover:bg-slate-100"
              >
                پاک کردن
              </button>
            )}
          </div>
        </form>
      </div>

      {/* Results */}
      <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">
        {isLoading ? (
          <TableSkeleton columns={10} rows={10} />
        ) : error ? (
          <div className="py-20 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-red-100 p-3 text-red-600">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z"
                />
              </svg>
            </div>
            <p className="text-slate-600">خطا در دریافت اطلاعات</p>
            <p className="mt-1 text-sm text-slate-400">{(error as Error).message}</p>
          </div>
        ) : !data || data.items.length === 0 ? (
          <div className="py-20 text-center">
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-slate-100 p-3 text-slate-400">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                />
              </svg>
            </div>
            <p className="text-slate-600">اصلاحی یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترهای خود را تغییر دهید' : 'برای شروع یک اصلاح جدید ایجاد کنید'}
            </p>
          </div>
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شناسه</th>
                    <SortableHeader
                      label="انبار"
                      sortKey="warehouseName"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('warehouseName' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="دلیل"
                      sortKey="reason"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('reason' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="وضعیت"
                      sortKey="status"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('status' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="تاریخ سند"
                      sortKey="docDate"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('docDate' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="تعداد آیتم"
                      sortKey="linesCount"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('linesCount' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="مجموع تغییر"
                      sortKey="totalQtyDelta"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('totalQtyDelta' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <SortableHeader
                      label="تاریخ ثبت"
                      sortKey="postedAt"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('postedAt' as keyof AdjustmentListItem)}
                      onSort={(key) => requestSort(key as keyof AdjustmentListItem)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sortedData.map((adjustment) => (
                    <tr
                      key={adjustment.id}
                      className="transition-colors hover:bg-slate-50 cursor-pointer"
                      onClick={() => navigate(`/adjustments/${adjustment.id}`)}
                    >
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-mono text-xs text-slate-500">{adjustmentDisplayNote(adjustment.id, adjustment.reason, adjustment.note)}</span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                        {adjustment.warehouseName || getWarehouseName(adjustment.warehouseId)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
                          {AdjustmentReasonLabels[adjustment.reason as AdjustmentReason] || adjustment.reason}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span
                          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${AdjustmentStatusColors[adjustment.status as AdjustmentStatus] || 'bg-slate-100 text-slate-800'
                            }`}
                        >
                          {AdjustmentStatusLabels[adjustment.status as AdjustmentStatus] || adjustment.status}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDate(adjustment.docDate)}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-center text-slate-700">
                        {adjustment.linesCount}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <span
                          className={`font-medium ${adjustment.totalQtyDelta > 0
                              ? 'text-emerald-600'
                              : adjustment.totalQtyDelta < 0
                                ? 'text-red-600'
                                : 'text-slate-600'
                            }`}
                        >
                          {adjustment.totalQtyDelta > 0 ? '+' : ''}
                          {adjustment.totalQtyDelta.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-500 text-xs">
                        {formatDateTime(adjustment.postedAt)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            navigate(`/adjustments/${adjustment.id}`)
                          }}
                          className="inline-flex items-center gap-1 rounded-lg bg-slate-100 px-2.5 py-1.5 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-200"
                        >
                          مشاهده
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-slate-50 px-4 py-3">
                <div className="text-sm text-slate-600">
                  نمایش {((data.page - 1) * data.pageSize) + 1} تا{' '}
                  {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')}{' '}
                  مورد
                </div>
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => handlePageChange(data.page - 1)}
                    disabled={data.page <= 1}
                    className="rounded-lg p-2 text-slate-600 transition-colors hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
                    </svg>
                  </button>

                  {/* Page numbers */}
                  {Array.from({ length: Math.min(5, data.totalPages) }, (_, i) => {
                    let pageNum: number
                    if (data.totalPages <= 5) {
                      pageNum = i + 1
                    } else if (data.page <= 3) {
                      pageNum = i + 1
                    } else if (data.page >= data.totalPages - 2) {
                      pageNum = data.totalPages - 4 + i
                    } else {
                      pageNum = data.page - 2 + i
                    }
                    return (
                      <button
                        key={pageNum}
                        onClick={() => handlePageChange(pageNum)}
                        className={`min-w-[36px] rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${data.page === pageNum
                            ? 'bg-emerald-600 text-white'
                            : 'text-slate-600 hover:bg-slate-200'
                          }`}
                      >
                        {pageNum.toLocaleString('fa-IR')}
                      </button>
                    )
                  })}

                  <button
                    onClick={() => handlePageChange(data.page + 1)}
                    disabled={data.page >= data.totalPages}
                    className="rounded-lg p-2 text-slate-600 transition-colors hover:bg-slate-200 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
                    </svg>
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  )
}

