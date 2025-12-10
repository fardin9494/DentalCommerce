import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useUnassignedStockItems, useMoveStockToShelf } from '../queries'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { AssignShelfModal } from '../components/AssignShelfModal'
import type { UnassignedStockItem, UnassignedStockItemsFilters } from '../api'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'

export function PutAwayPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()
  const moveToShelf = useMoveStockToShelf()
  const { getWarehouseName } = useWarehouseNames()

  // Get filters from URL
  const filters: UnassignedStockItemsFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
      warehouseId: searchParams.get('warehouseId') || undefined,
      search: searchParams.get('search') || undefined,
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useUnassignedStockItems(filters)

  // Local filter states
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')
  const [assigningItem, setAssigningItem] = useState<UnassignedStockItem | null>(null)

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => {
      const currentSearch = searchParams.get('search') || ''
      if (searchInput !== currentSearch) {
        const params = new URLSearchParams(searchParams)
        if (searchInput.trim()) {
          params.set('search', searchInput.trim())
        } else {
          params.delete('search')
        }
        params.set('page', '1')
        setSearchParams(params, { replace: true })
      }
    }, 300)

    return () => clearTimeout(timer)
  }, [searchInput, searchParams, setSearchParams])

  // Sync searchInput with URL
  useEffect(() => {
    const urlSearch = searchParams.get('search') || ''
    if (urlSearch !== searchInput) {
      setSearchInput(urlSearch)
    }
  }, [searchParams])

  function updateFilters(newFilters: Partial<UnassignedStockItemsFilters>) {
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

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setWarehouseFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleAssignShelf(data: { shelfId: string; qty: number; note?: string }) {
    if (!assigningItem) return
    await moveToShelf.mutateAsync({
      sourceStockItemId: assigningItem.id,
      targetShelfId: data.shelfId,
      qty: data.qty,
      note: data.note,
    })
    setAssigningItem(null)
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

  const hasActiveFilters = warehouseFilter || searchInput
  // برای موجودی‌های جدید (مسدود)، باید از Blocked استفاده کنیم
  const totalUnassigned = data?.items.reduce((sum, item) => sum + (item.blocked > 0 ? item.blocked : item.available), 0) ?? 0

  return (
    <div className="space-y-6">
      <PageHeader title="چیدن کالا در قفسه‌ها">انتساب کالاهای بدون قفسه به قفسه‌های انبار</PageHeader>

      {/* Stats Card */}
      <div className="rounded-xl border border-orange-200 bg-orange-50 p-4 shadow-sm">
        <div className="flex items-center justify-between">
          <div>
            <div className="text-sm font-medium text-orange-600">کالاهای بدون قفسه</div>
            <div className="mt-2 text-3xl font-bold text-orange-700">
              {data?.totalCount ?? 0} آیتم
            </div>
            <div className="mt-1 text-sm text-orange-600">
              مجموع موجودی: {totalUnassigned.toLocaleString('fa-IR')}
            </div>
          </div>
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-orange-100">
            <svg className="h-8 w-8 text-orange-600" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
            </svg>
          </div>
        </div>
      </div>

      {/* Filters */}
      <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
        <form
          onSubmit={(e) => {
            e.preventDefault()
            updateFilters({ search: searchInput || undefined })
          }}
          className="flex flex-wrap items-end gap-4"
        >
          {/* Search */}
          <div className="flex-1 min-w-[200px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">جستجو</label>
            <div className="relative">
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="نام محصول، SKU یا شماره لات..."
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
            <div className="mx-auto mb-4 h-12 w-12 rounded-full bg-emerald-100 p-3 text-emerald-600">
              <svg fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"
                />
              </svg>
            </div>
            <p className="text-slate-600">همه کالاها در قفسه‌ها چیده شده‌اند</p>
            <p className="mt-1 text-sm text-slate-400">کالای بدون قفسه وجود ندارد</p>
          </div>
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">محصول</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">SKU</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">انبار</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">لات</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">انقضا</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600 text-center">موجودی آزاد</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.items.map((item) => (
                    <tr key={item.id} className="transition-colors hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <div>
                          <div className="font-medium text-slate-900">{item.productName || 'نامشخص'}</div>
                          {item.variantValue && (
                            <div className="mt-0.5 text-sm text-emerald-600">واریانت: {item.variantValue}</div>
                          )}
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-mono text-sm font-medium text-slate-700">{item.sku}</span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                        {item.warehouseName || getWarehouseName(item.warehouseId)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{item.lotNumber || '-'}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDate(item.expiryDate)}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        {item.blocked > 0 ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-orange-100 px-2.5 py-0.5 text-xs font-medium text-orange-700">
                            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
                            </svg>
                            {item.blocked.toLocaleString('fa-IR')} (مسدود)
                          </span>
                        ) : (
                          <span className="font-semibold text-emerald-600">{item.available.toLocaleString('fa-IR')}</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <button
                          onClick={() => setAssigningItem(item)}
                          disabled={item.blocked === 0 && item.available <= 0}
                          className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
                          </svg>
                          چیدن در قفسه
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
                  {Math.min(data.page * data.pageSize, data.totalCount)} از {data.totalCount.toLocaleString('fa-IR')} آیتم
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

      {/* Assign Shelf Modal */}
      <AssignShelfModal
        isOpen={!!assigningItem}
        stockItem={assigningItem}
        onClose={() => setAssigningItem(null)}
        onSubmit={handleAssignShelf}
        isSubmitting={moveToShelf.isPending}
      />
    </div>
  )
}

