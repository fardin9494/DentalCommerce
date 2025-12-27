import { useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import { useReceiptRejectionsList, useResolveReceiptRejection } from '../queries'
import {
  ReceiptRejectionStatusLabels,
  ReceiptRejectionStatusColors,
  ReceiptRejectionStatusMap,
  type ReceiptRejectionStatus,
  type ReceiptRejectionsListFilters,
} from '../types'
import { useWarehouseNames, useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { useSortableTable } from '@/shared/hooks/useSortableTable'
import { SortableHeader } from '@/shared/components/SortableHeader'
import { receiptDisplayRef } from '@/shared/utils/inventoryDocumentReference'
import { ResolveReceiptRejectionModal } from '../components/ResolveReceiptRejectionModal'

interface ReceiptRejectionRow {
  receiptLineId: string
  receiptId: string
  warehouseId: string
  warehouseName?: string | null
  receiptReason: string
  receiptStatus: string
  receiptExternalRef?: string | null
  receiptDocDate: string
  lineNo: number
  productId: string
  variantId?: string | null
  lotNumber?: string | null
  expiryDateUtc?: string | null
  unitCost?: number | null
  rejectedQty: number
  rejectionApprovedQty: number
  rejectionReturnedQty: number
  rejectionDisposedQty: number
  rejectionReason?: string | null
  rejectionStatus: string
  rejectionResolvedAt?: string | null
  rejectionResolutionNote?: string | null
}

export function ReceiptRejectionsListPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()
  const { getWarehouseName } = useWarehouseNames()

  const filters: ReceiptRejectionsListFilters = useMemo(() => ({
    page: parseInt(searchParams.get('page') || '1', 10),
    pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
    status: searchParams.get('status') ? parseInt(searchParams.get('status')!, 10) : undefined,
    search: searchParams.get('search') || undefined,
    warehouseId: searchParams.get('warehouseId') || undefined,
  }), [searchParams])

  const { data, isLoading, error } = useReceiptRejectionsList(filters)

  const { sortedData, requestSort, getSortIndicator, sortConfig } = useSortableTable<ReceiptRejectionRow>({
    data: (data?.items || []) as ReceiptRejectionRow[],
    defaultSortKey: 'receiptDocDate',
    defaultDirection: 'desc',
  })

  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [statusFilter, setStatusFilter] = useState<string>(filters.status?.toString() || '')
  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')

  const productIds = useMemo(() => data?.items.map((item) => item.productId) || [], [data?.items])
  const { getProductName, getVariantName } = useProductNames(productIds)

  const resolveRejection = useResolveReceiptRejection()
  const isMutating = resolveRejection.isPending
  const [resolveTarget, setResolveTarget] = useState<ReceiptRejectionRow | null>(null)

  function updateFilters(newFilters: Partial<ReceiptRejectionsListFilters>) {
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

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    updateFilters({ search: searchInput || undefined })
  }

  function handleStatusChange(value: string) {
    setStatusFilter(value)
    updateFilters({ status: value ? parseInt(value, 10) : undefined })
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
    setWarehouseFilter('')
    setSearchParams(new URLSearchParams())
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

  function formatQty(value: number) {
    return value.toLocaleString('fa-IR')
  }

  async function handleResolveSubmit(payload: {
    approvedQty: number
    returnedQty: number
    disposedQty: number
    note?: string
  }) {
    if (!resolveTarget) return
    await resolveRejection.mutateAsync({
      receiptLineId: resolveTarget.receiptLineId,
      approvedQty: payload.approvedQty,
      returnedQty: payload.returnedQty,
      disposedQty: payload.disposedQty,
      note: payload.note,
    })
  }

  function handleOpenResolve(item: ReceiptRejectionRow) {
    setResolveTarget(item)
  }

  const hasActiveFilters = statusFilter || searchInput || warehouseFilter

  return (
    <div className="space-y-6">
      <PageHeader title="اقلام رد شده رسیدها">
        بررسی و تعیین تکلیف اقلام رد شده در رسیدهای انبار
      </PageHeader>

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-5 shadow-md">
        <form onSubmit={handleSearch} className="flex flex-wrap items-end gap-4">
          <div className="flex-1 min-w-[200px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">جستجو</label>
            <div className="relative">
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="شماره رسید، شناسه یا لات..."
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

          <div className="min-w-[180px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">انبار</label>
            <select
              value={warehouseFilter}
              onChange={(e) => handleWarehouseChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه انبارها</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>

          <div className="min-w-[170px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">وضعیت رسیدگی</label>
            <select
              value={statusFilter}
              onChange={(e) => handleStatusChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه</option>
              <option value={ReceiptRejectionStatusMap.Pending}>در انتظار رسیدگی</option>
              <option value={ReceiptRejectionStatusMap.ApprovedToStock}>تایید و افزودن به انبار</option>
              <option value={ReceiptRejectionStatusMap.Returned}>مرجوعی</option>
              <option value={ReceiptRejectionStatusMap.Disposed}>معدوم</option>
              <option value={ReceiptRejectionStatusMap.Mixed}>ترکیبی</option>
            </select>
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700"
            >
              جستجو
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
          <TableSkeleton columns={9} rows={10} />
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
                  d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 0 1-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                />
              </svg>
            </div>
            <p className="text-slate-600">آیتمی یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترها را تغییر دهید' : 'هیچ قلم رد شده‌ای برای نمایش وجود ندارد'}
            </p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">رسید</th>
                    <SortableHeader
                      label="انبار"
                      sortKey="warehouseId"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('warehouseId' as keyof ReceiptRejectionRow)}
                      onSort={(key) => requestSort(key as keyof ReceiptRejectionRow)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">کالا</th>
                    <SortableHeader
                      label="مقدار رد شده"
                      sortKey="rejectedQty"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('rejectedQty' as keyof ReceiptRejectionRow)}
                      onSort={(key) => requestSort(key as keyof ReceiptRejectionRow)}
                      className="text-center"
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">دلیل رد</th>
                    <SortableHeader
                      label="وضعیت"
                      sortKey="rejectionStatus"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('rejectionStatus' as keyof ReceiptRejectionRow)}
                      onSort={(key) => requestSort(key as keyof ReceiptRejectionRow)}
                    />
                    <SortableHeader
                      label="تاریخ رسید"
                      sortKey="receiptDocDate"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('receiptDocDate' as keyof ReceiptRejectionRow)}
                      onSort={(key) => requestSort(key as keyof ReceiptRejectionRow)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sortedData.map((item) => (
                    <tr key={item.receiptLineId} className="transition-colors hover:bg-slate-50">
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-mono text-xs text-slate-500">
                          {receiptDisplayRef(item.receiptId, item.receiptReason, item.receiptExternalRef)}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                        {item.warehouseName || getWarehouseName(item.warehouseId)}
                      </td>
                      <td className="px-4 py-3">
                        <div className="font-medium text-slate-900">{getProductName(item.productId)}</div>
                        {item.variantId ? (
                          <div className="mt-0.5 text-xs text-emerald-600">
                            {getVariantName(item.variantId) || item.variantId.substring(0, 8) + '...'}
                          </div>
                        ) : null}
                        <div className="mt-1 text-xs text-slate-500">
                          لات: {item.lotNumber || '-'} | انقضا: {formatDate(item.expiryDateUtc)}
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <div className="flex flex-col items-center gap-1">
                          <span className="inline-flex items-center rounded-full bg-red-100 px-2.5 py-0.5 text-xs font-medium text-red-700">
                            {item.rejectedQty.toLocaleString('fa-IR')}
                          </span>
                          <span className="text-[11px] text-slate-500">
                            باقیمانده:{' '}
                            {(item.rejectedQty - (item.rejectionApprovedQty + item.rejectionReturnedQty + item.rejectionDisposedQty)).toLocaleString('fa-IR')}
                          </span>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-slate-600">
                        <span title={item.rejectionReason || undefined}>
                          {item.rejectionReason
                            ? item.rejectionReason.length > 40
                              ? `${item.rejectionReason.substring(0, 40)}...`
                              : item.rejectionReason
                            : '-'}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="flex flex-col gap-1">
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${ReceiptRejectionStatusColors[item.rejectionStatus as ReceiptRejectionStatus] || 'bg-slate-100 text-slate-700'
                              }`}
                          >
                            {ReceiptRejectionStatusLabels[item.rejectionStatus as ReceiptRejectionStatus] || item.rejectionStatus}
                          </span>
                          {item.rejectionResolvedAt && (
                            <span className="text-xs text-slate-500">{formatDateTime(item.rejectionResolvedAt)}</span>
                          )}
                          {item.rejectionStatus === 'Mixed' && (
                            <div className="mt-1 flex flex-wrap gap-1 text-[11px] text-slate-600">
                              {item.rejectionApprovedQty > 0 && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-1.5 py-0.5">
                                  <span>تایید شده</span>
                                  <span className="font-medium text-emerald-700">{formatQty(item.rejectionApprovedQty)}</span>
                                </span>
                              )}
                              {item.rejectionReturnedQty > 0 && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-blue-50 px-1.5 py-0.5">
                                  <span>مرجوع</span>
                                  <span className="font-medium text-blue-700">{formatQty(item.rejectionReturnedQty)}</span>
                                </span>
                              )}
                              {item.rejectionDisposedQty > 0 && (
                                <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-1.5 py-0.5">
                                  <span>معدوم</span>
                                  <span className="font-medium text-red-700">{formatQty(item.rejectionDisposedQty)}</span>
                                </span>
                              )}
                            </div>
                          )}
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                        {formatDate(item.receiptDocDate)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {item.rejectionStatus === 'Pending' ? (
                          <button
                            onClick={() => handleOpenResolve(item)}
                            disabled={isMutating || item.rejectedQty <= item.rejectionApprovedQty + item.rejectionReturnedQty + item.rejectionDisposedQty}
                            className="rounded-lg bg-emerald-50 px-2.5 py-1 text-xs font-medium text-emerald-700 transition-colors hover:bg-emerald-100 disabled:opacity-50"
                          >
                            تعیین تکلیف
                          </button>
                        ) : (
                          <span className="text-xs text-slate-400">-</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-slate-50 px-4 py-3">
                <div className="text-sm text-slate-600">
                  نمایش {((data.page - 1) * data.pageSize) + 1} تا {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')}
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
      <ResolveReceiptRejectionModal
        isOpen={!!resolveTarget}
        item={resolveTarget}
        onClose={() => setResolveTarget(null)}
        onSubmit={handleResolveSubmit}
        isSubmitting={isMutating}
      />
    </div>
  )
}
