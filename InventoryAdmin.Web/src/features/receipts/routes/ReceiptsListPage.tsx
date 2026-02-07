import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import { useReceiptsList, useCreateReceipt } from '../queries'
import { CreateReceiptModal } from '../components/CreateReceiptModal'
import {
  ReceiptStatusLabels,
  ReceiptReasonLabels,
  ReceiptStatusColors,
  type ReceiptStatus,
  type ReceiptReason,
  type ReceiptsListFilters,
} from '../types'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useSortableTable } from '@/shared/hooks/useSortableTable'
import { SortableHeader } from '@/shared/components/SortableHeader'
import { receiptDisplayRef } from '@/shared/utils/inventoryDocumentReference'
import { PermissionGate } from '@/app/permissions'
import { InventoryPermissionKeys } from '@/app/inventoryPermissionKeys'

// Type for receipt list item
interface ReceiptListItem {
  id: string
  warehouseId: string
  warehouseName?: string | null
  reason: string
  status: string
  docDate: string
  externalRef?: string | null
  linesCount: number
  totalQty: number
  receivedAt?: string | null
}

export function ReceiptsListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const createReceipt = useCreateReceipt()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [creating, setCreating] = useState(false)
  const { getWarehouseName } = useWarehouseNames()

  // Get filters from URL
  const filters: ReceiptsListFilters = useMemo(() => ({
    page: parseInt(searchParams.get('page') || '1', 10),
    pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
    status: searchParams.get('status') ? parseInt(searchParams.get('status')!, 10) : undefined,
    reason: searchParams.get('reason') ? parseInt(searchParams.get('reason')!, 10) : undefined,
    search: searchParams.get('search') || undefined,
    warehouseId: searchParams.get('warehouseId') || undefined,
  }), [searchParams])

  const { data, isLoading, error } = useReceiptsList(filters)

  // Sorting - default by docDate descending (newest first)
  const { sortedData, requestSort, getSortIndicator, sortConfig } = useSortableTable<ReceiptListItem>({
    data: data?.items || [],
    defaultSortKey: 'docDate',
    defaultDirection: 'desc',
  })

  // Local filter states for UI
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [statusFilter, setStatusFilter] = useState<string>(filters.status?.toString() || '')
  const [reasonFilter, setReasonFilter] = useState<string>(filters.reason?.toString() || '')

  function updateFilters(newFilters: Partial<ReceiptsListFilters>) {
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

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setStatusFilter('')
    setReasonFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleCreateReceipt(data: { warehouseId: string; reason: number; externalRef?: string }) {
    setCreating(true)
    try {
      const result = await createReceipt.mutateAsync({
        warehouseId: data.warehouseId,
        reason: data.reason,
        externalRef: data.externalRef || null,
        docDateUtc: new Date().toISOString(),
      })
      setShowCreateModal(false)
      navigate(`/receipts/${result.id}`)
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

  const hasActiveFilters = statusFilter || reasonFilter || searchInput

  return (
    <div className="space-y-6">
      <PageHeader
        title="رسیدها"
        actions={
          <PermissionGate permission={InventoryPermissionKeys.ReceiptsCreate}>
            <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            رسید جدید
          </button>
          </PermissionGate>
        }
      >
        مدیریت رسیدهای ورود به انبار
      </PageHeader>

      {/* Create Receipt Modal */}
      <CreateReceiptModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateReceipt}
        isSubmitting={creating}
      />

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-md">
        <form onSubmit={handleSearch} className="flex flex-wrap items-end gap-4">
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">جستجو</label>
            <div className="relative">
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="شماره مرجع یا شناسه..."
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

          {/* Status Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">وضعیت</label>
            <select
              value={statusFilter}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            >
              <option value="">همه</option>
              <option value="1">پیش‌نویس</option>
              <option value="2">دریافت شده</option>
              <option value="3">تایید شده</option>
              <option value="4">لغو شده</option>
            </select>
          </div>

          {/* Reason Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">نوع</label>
            <select
              value={reasonFilter}
              onChange={(e) => handleReasonChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            >
              <option value="">همه</option>
              <option value="1">خرید</option>
              <option value="2">مرجوعی</option>
              <option value="3">تولید</option>
              <option value="99">سایر</option>
            </select>
          </div>

          {/* Buttons */}
          <div className="flex gap-2">
            <button
              type="submit"
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700"
            >
              اعمال
            </button>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 border border-slate-200"
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
            <p className="text-slate-600">رسیدی یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترهای خود را تغییر دهید' : 'برای شروع یک رسید جدید ایجاد کنید'}
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
                      sortKey="warehouseId"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('warehouseId' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="نوع"
                      sortKey="reason"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('reason' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="وضعیت"
                      sortKey="status"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('status' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="تاریخ سند"
                      sortKey="docDate"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('docDate' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="تعداد آیتم"
                      sortKey="linesCount"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('linesCount' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="مجموع تعداد"
                      sortKey="totalQty"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('totalQty' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <SortableHeader
                      label="تاریخ دریافت"
                      sortKey="receivedAt"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('receivedAt' as keyof ReceiptListItem)}
                      onSort={(key) => requestSort(key as keyof ReceiptListItem)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sortedData.map((receipt) => (
                    <tr
                      key={receipt.id}
                      className="transition-colors hover:bg-slate-50 cursor-pointer even:bg-slate-50/60"
                      onClick={() => navigate(`/receipts/${receipt.id}`)}
                    >
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-mono text-xs text-slate-500">
                          {receiptDisplayRef(receipt.id, receipt.reason, receipt.externalRef)}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                        {receipt.warehouseName || getWarehouseName(receipt.warehouseId)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="inline-flex items-center rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700">
                          {ReceiptReasonLabels[receipt.reason as ReceiptReason]}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span
                          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ReceiptStatusColors[receipt.status as ReceiptStatus]
                            }`}
                        >
                          {ReceiptStatusLabels[receipt.status as ReceiptStatus]}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                        {formatDate(receipt.docDate)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center text-slate-700">
                        {receipt.linesCount}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center text-slate-700">
                        {receipt.totalQty.toLocaleString('fa-IR')}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-500 text-xs">
                        {formatDateTime(receipt.receivedAt)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <button
                          onClick={(e) => {
                            e.stopPropagation()
                            navigate(`/receipts/${receipt.id}`)
                          }}
                          className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
                        >
                          <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                            <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                          </svg>
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
                  {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')} رسید
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

