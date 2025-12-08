import { useState, useMemo } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useTransfersList, useCreateTransfer } from '../queries'
import { CreateTransferModal } from '../components/CreateTransferModal'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import {
  TransferStatusLabels,
  TransferStatusColors,
  TransferStatusMap,
  type TransferStatus,
  type TransfersListFilters,
} from '../types'

export function TransfersListPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const createTransfer = useCreateTransfer()
  const { data: warehouses } = useActiveWarehouses()
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [creating, setCreating] = useState(false)

  // Get filters from URL
  const filters: TransfersListFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
      status: searchParams.get('status') ? parseInt(searchParams.get('status')!, 10) : undefined,
      sourceWarehouseId: searchParams.get('sourceWarehouseId') || undefined,
      destinationWarehouseId: searchParams.get('destinationWarehouseId') || undefined,
      search: searchParams.get('search') || undefined,
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useTransfersList(filters)

  // Local filter states for UI
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [statusFilter, setStatusFilter] = useState<string>(filters.status?.toString() || '')
  const [sourceWarehouseFilter, setSourceWarehouseFilter] = useState<string>(filters.sourceWarehouseId || '')
  const [destinationWarehouseFilter, setDestinationWarehouseFilter] = useState<string>(
    filters.destinationWarehouseId || ''
  )

  function updateFilters(newFilters: Partial<TransfersListFilters>) {
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

  function handleSourceWarehouseChange(value: string) {
    setSourceWarehouseFilter(value)
    updateFilters({ sourceWarehouseId: value || undefined })
  }

  function handleDestinationWarehouseChange(value: string) {
    setDestinationWarehouseFilter(value)
    updateFilters({ destinationWarehouseId: value || undefined })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setStatusFilter('')
    setSourceWarehouseFilter('')
    setDestinationWarehouseFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleCreateTransfer(data: {
    sourceWarehouseId: string
    destinationWarehouseId: string
    externalRef?: string
  }) {
    setCreating(true)
    try {
      const result = await createTransfer.mutateAsync({
        sourceWarehouseId: data.sourceWarehouseId,
        destinationWarehouseId: data.destinationWarehouseId,
        externalRef: data.externalRef || null,
        docDateUtc: new Date().toISOString(),
      })
      setShowCreateModal(false)
      navigate(`/transfers/${result.id}`)
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

  const hasActiveFilters = statusFilter || sourceWarehouseFilter || destinationWarehouseFilter || searchInput

  return (
    <div className="space-y-6">
      <PageHeader
        title="انتقالات"
        actions={
          <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:ring-offset-2"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
            </svg>
            انتقال جدید
          </button>
        }
      >
        مدیریت انتقالات بین انبارها
      </PageHeader>

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
                placeholder="شماره سند، مرجع خارجی..."
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

          {/* Source Warehouse Filter */}
          <div className="min-w-[180px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">انبار مبدا</label>
            <select
              value={sourceWarehouseFilter}
              onChange={(e) => handleSourceWarehouseChange(e.target.value)}
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

          {/* Destination Warehouse Filter */}
          <div className="min-w-[180px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">انبار مقصد</label>
            <select
              value={destinationWarehouseFilter}
              onChange={(e) => handleDestinationWarehouseChange(e.target.value)}
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
              {Object.entries(TransferStatusLabels).map(([key, label]) => (
                <option key={key} value={TransferStatusMap[key as TransferStatus]}>
                  {label}
                </option>
              ))}
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
          <div className="flex items-center justify-center py-20">
            <Spinner className="h-8 w-8 text-emerald-600" />
          </div>
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
                  d="M7.5 21 3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5"
                />
              </svg>
            </div>
            <p className="text-slate-600">انتقالی یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترهای خود را تغییر دهید' : 'هنوز انتقالی ثبت نشده است'}
            </p>
          </div>
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شماره سند</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">انبار مبدا</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">انبار مقصد</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">وضعیت</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تاریخ</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تعداد خطوط</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">مقدار کل</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">تخصیص یافته</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">باقی‌مانده</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.items.map((item) => {
                    const status = item.status as TransferStatus
                    return (
                      <tr
                        key={item.id}
                        className="transition-colors hover:bg-slate-50 cursor-pointer"
                        onClick={() => navigate(`/transfers/${item.id}`)}
                      >
                        <td className="px-4 py-3">
                          <div className="font-medium text-slate-900">
                            {item.externalRef || item.id.substring(0, 8) + '...'}
                          </div>
                          <div className="text-xs text-slate-400 font-mono">{item.id.substring(0, 8)}...</div>
                        </td>
                        <td className="px-4 py-3 text-slate-700">{item.sourceWarehouseName || '-'}</td>
                        <td className="px-4 py-3 text-slate-700">{item.destinationWarehouseName || '-'}</td>
                        <td className="px-4 py-3">
                          <span
                            className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                              TransferStatusColors[status] || 'bg-gray-100 text-gray-800'
                            }`}
                          >
                            {TransferStatusLabels[status] || item.status}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-slate-600">{formatDate(item.docDate)}</td>
                        <td className="px-4 py-3 text-center text-slate-600">{item.linesCount}</td>
                        <td className="px-4 py-3 text-center font-medium text-slate-900">
                          {item.totalQty.toLocaleString('fa-IR')}
                        </td>
                        <td className="px-4 py-3 text-center text-slate-600">
                          {item.totalAllocatedQty.toLocaleString('fa-IR')}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span
                            className={`font-medium ${
                              item.totalRemainingQty > 0 ? 'text-orange-600' : 'text-emerald-600'
                            }`}
                          >
                            {item.totalRemainingQty.toLocaleString('fa-IR')}
                          </span>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-slate-200 bg-slate-50 px-4 py-3">
                <div className="text-sm text-slate-600">
                  نمایش {((data.page - 1) * data.pageSize) + 1} تا{' '}
                  {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')}{' '}
                  آیتم
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
                        className={`min-w-[36px] rounded-lg px-3 py-1.5 text-sm font-medium transition-colors ${
                          data.page === pageNum
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

      {/* Create Modal */}
      <CreateTransferModal
        isOpen={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        onSubmit={handleCreateTransfer}
        isSubmitting={creating}
      />
    </div>
  )
}
