import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { Spinner } from '@/shared/components/Spinner'
import { useStockItems } from '../queries'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { useWarehouseNames } from '@/shared/hooks/useWarehouses'
import { useProductNames } from '@/shared/hooks/useProductNames'
import { useSortableTable } from '@/shared/hooks/useSortableTable'
import { SortableHeader } from '@/shared/components/SortableHeader'
import type { StockItemsListFilters } from '../api'

// Type for stock item (matching API response)
interface StockItem {
  id: string
  productId: string
  productName?: string | null
  variantId?: string | null
  variantValue?: string | null
  sku: string
  warehouseId: string
  warehouseName?: string | null
  shelfId?: string | null
  shelfName?: string | null
  lotNumber?: string | null
  expiryDate?: string | null
  onHand: number
  reserved: number
  blocked: number
  available: number
  blockReason?: string | null
  createdAt: string
  updatedAt: string
}

export function StockItemsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()

  // Local filter states - initialize from URL
  const initialSearch = searchParams.get('search') || ''
  const [searchInput, setSearchInput] = useState(initialSearch)

  // Debounce search input for real-time search and update URL
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
        params.set('page', '1') // Reset to page 1 on search
        setSearchParams(params, { replace: true })
      }
    }, 300) // 300ms debounce

    return () => clearTimeout(timer)
  }, [searchInput, searchParams, setSearchParams])

  // Sync searchInput with URL when it changes externally (e.g., browser back/forward)
  useEffect(() => {
    const urlSearch = searchParams.get('search') || ''
    if (urlSearch !== searchInput) {
      setSearchInput(urlSearch)
    }
  }, [searchParams])

  // Get filters from URL
  // Default: hasStock = true (only show items with stock > 0)
  const filters: StockItemsListFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
      warehouseId: searchParams.get('warehouseId') || undefined,
      search: searchParams.get('search') || undefined,
      hasStock: searchParams.get('hasStock') === 'false' ? false : true, // Default to true
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useStockItems(filters)

  // Sorting - default by last update (fallback to creation)
  const { sortedData, requestSort, getSortIndicator, sortConfig } = useSortableTable<StockItem>({
    data: (data?.items || []) as StockItem[],
    defaultSortKey: 'updatedAt',
    defaultDirection: 'desc',
  })

  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')
  const [hasStockFilter, setHasStockFilter] = useState<string>(
    filters.hasStock === false ? 'false' : 'true' // Default to 'true'
  )

  // جمع‌آوری productId های تمام آیتم‌ها برای fetch کردن نام محصولات
  const productIds = useMemo(() => data?.items.map((item) => item.productId) || [], [data?.items])
  const { getVariantName } = useProductNames(productIds)
  const { getWarehouseName } = useWarehouseNames()

  function updateFilters(newFilters: Partial<StockItemsListFilters>) {
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
    // Search is handled by debounce in useEffect, but we can update immediately if user presses Enter
    updateFilters({ search: searchInput || undefined })
  }

  function handleWarehouseChange(value: string) {
    setWarehouseFilter(value)
    updateFilters({ warehouseId: value || undefined })
  }

  function handleHasStockChange(value: string) {
    setHasStockFilter(value)
    updateFilters({
      hasStock: value === 'true' ? true : value === 'false' ? false : undefined,
    })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function clearFilters() {
    setSearchInput('')
    setWarehouseFilter('')
    setHasStockFilter('true') // Reset to default (show items with stock)
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

  function truncateProductName(name: string | null | undefined, maxLength: number = 20): string {
    if (!name) return 'نامشخص'
    if (name.length <= maxLength) return name
    return name.substring(0, maxLength) + '...'
  }

  // hasStockFilter is 'true' by default, so only count it as active if it's explicitly set to 'false'
  const hasActiveFilters = warehouseFilter || (hasStockFilter === 'false') || searchInput
  const totalOnHand = data?.items.reduce((sum, item) => sum + item.onHand, 0) ?? 0
  const totalAvailable = data?.items.reduce((sum, item) => sum + item.available, 0) ?? 0

  return (
    <div className="space-y-6">
      <PageHeader title="موجودی‌های انبار">مدیریت و مشاهده موجودی‌های کالا در انبارها</PageHeader>

      {/* Stats Cards */}
      <div className="grid gap-4 sm:grid-cols-4">
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">کل آیتم‌ها</div>
          <div className="mt-2 text-2xl font-bold text-slate-900">{data?.totalCount ?? 0}</div>
        </div>
        <div className="rounded-xl border border-blue-200 bg-blue-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-blue-600">موجودی کل</div>
          <div className="mt-2 text-2xl font-bold text-blue-700">{totalOnHand.toLocaleString('fa-IR')}</div>
        </div>
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 p-4 shadow-sm">
          <div className="text-sm font-medium text-emerald-600">موجودی آزاد</div>
          <div className="mt-2 text-2xl font-bold text-emerald-700">{totalAvailable.toLocaleString('fa-IR')}</div>
        </div>
        <div className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
          <div className="text-sm font-medium text-slate-500">رزرو شده</div>
          <div className="mt-2 text-2xl font-bold text-slate-900">
            {data?.items.reduce((sum, item) => sum + item.reserved, 0).toLocaleString('fa-IR') ?? 0}
          </div>
        </div>
      </div>

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

          {/* Has Stock Filter */}
          <div className="min-w-[150px]">
            <label className="mb-1.5 block text-sm font-medium text-slate-700">وضعیت موجودی</label>
            <select
              value={hasStockFilter}
              onChange={(e) => handleHasStockChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 py-2 px-3 text-sm focus:border-emerald-500 focus:outline-none focus:ring-1 focus:ring-emerald-500"
            >
              <option value="">همه</option>
              <option value="true">دارای موجودی</option>
              <option value="false">بدون موجودی</option>
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
                  d="M20.25 7.5l-.625 10.632a2.25 2.25 0 01-2.247 2.118H6.622a2.25 2.25 0 01-2.247-2.118L3.75 7.5M10 11.25h4M3.375 7.5h17.25c.621 0 1.125-.504 1.125-1.125v-1.5c0-.621-.504-1.125-1.125-1.125H3.375c-.621 0-1.125.504-1.125 1.125v1.5c0 .621.504 1.125 1.125 1.125z"
                />
              </svg>
            </div>
            <p className="text-slate-600">موجودی یافت نشد</p>
            <p className="mt-1 text-sm text-slate-400">
              {hasActiveFilters ? 'فیلترهای خود را تغییر دهید' : 'هنوز موجودی ثبت نشده است'}
            </p>
          </div>
        ) : (
          <>
            {/* Table */}
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <SortableHeader
                      label="محصول"
                      sortKey="productName"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('productName' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                    />
                    <SortableHeader
                      label="SKU"
                      sortKey="sku"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('sku' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                    />
                    <SortableHeader
                      label="انبار"
                      sortKey="warehouseName"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('warehouseName' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                    />
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">قفسه</th>
                    <SortableHeader
                      label="لات"
                      sortKey="lotNumber"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('lotNumber' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                    />
                    <SortableHeader
                      label="انقضا"
                      sortKey="expiryDate"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('expiryDate' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                    />
                    <SortableHeader
                      label="موجودی"
                      sortKey="onHand"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('onHand' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                      className="text-center"
                    />
                    <SortableHeader
                      label="رزرو"
                      sortKey="reserved"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('reserved' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                      className="text-center"
                    />
                    <SortableHeader
                      label="مسدود"
                      sortKey="blocked"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('blocked' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                      className="text-center"
                    />
                    <SortableHeader
                      label="آزاد"
                      sortKey="available"
                      currentSortKey={sortConfig.key as string}
                      currentDirection={getSortIndicator('available' as keyof StockItem)}
                      onSort={(key) => requestSort(key as keyof StockItem)}
                      className="text-center"
                    />
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {sortedData.map((item) => (
                    <tr key={item.id} className="transition-colors hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <div>
                          <div className="font-medium text-slate-900" title={item.productName || undefined}>
                            {truncateProductName(item.productName)}
                          </div>
                          {item.variantValue && (
                            <div className="mt-0.5 text-sm text-emerald-600">واریانت: {truncateProductName(item.variantValue, 15)}</div>
                          )}
                          {!item.variantValue && item.variantId && (
                            <div className="mt-0.5 text-sm text-emerald-600">
                              واریانت: {getVariantName(item.variantId) || item.variantId.substring(0, 8) + '...'}
                            </div>
                          )}
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-mono text-sm font-medium text-slate-700">{item.sku}</span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-700">
                        {item.warehouseName || getWarehouseName(item.warehouseId)}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {item.shelfName ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-medium text-emerald-700">
                            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                              <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
                            </svg>
                            {item.shelfName}
                          </span>
                        ) : (
                          <span className="text-xs text-slate-400">-</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{item.lotNumber || '-'}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{formatDate(item.expiryDate)}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-center font-medium text-slate-900">
                        {item.onHand.toLocaleString('fa-IR')}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center text-slate-600">
                        {item.reserved.toLocaleString('fa-IR')}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        {item.blocked > 0 ? (
                          <span className="inline-flex items-center gap-1 rounded-full bg-orange-100 px-2 py-0.5 text-xs font-medium text-orange-700">
                            {item.blocked.toLocaleString('fa-IR')}
                            {item.blockReason && (
                              <span title={item.blockReason} className="cursor-help">
                                ⚠️
                              </span>
                            )}
                          </span>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-center">
                        <span
                          className={`font-semibold ${item.available > 0 ? 'text-emerald-600' : item.available < 0 ? 'text-red-600' : 'text-slate-400'
                            }`}
                        >
                          {item.available.toLocaleString('fa-IR')}
                        </span>
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
