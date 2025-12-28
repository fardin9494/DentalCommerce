import { useState, useMemo, useEffect } from 'react'
import { useSearchParams } from 'react-router-dom'
import { PageHeader } from '@/shared/components/PageHeader'
import { TableSkeleton } from '@/shared/components/TableSkeleton'
import { useAssignedStockItems, useMoveStockBetweenShelves } from '../queries'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { TransferShelfModal } from '../components/TransferShelfModal'
import type { StockItem } from '../../stock-items/api'
import type { StockItemsListFilters } from '../../stock-items/api'

export function ShelfTransferPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const { data: warehouses } = useActiveWarehouses()
  const moveStock = useMoveStockBetweenShelves()

  // Get filters from URL
  const filters: StockItemsListFilters = useMemo(
    () => ({
      page: parseInt(searchParams.get('page') || '1', 10),
      pageSize: parseInt(searchParams.get('pageSize') || '20', 10),
      warehouseId: searchParams.get('warehouseId') || undefined,
      search: searchParams.get('search') || undefined,
      hasStock: true, // Only show items with stock
    }),
    [searchParams]
  )

  const { data, isLoading, error } = useAssignedStockItems(filters)

  // Local filter states
  const [searchInput, setSearchInput] = useState(filters.search || '')
  const [warehouseFilter, setWarehouseFilter] = useState<string>(filters.warehouseId || '')
  const [transferringItem, setTransferringItem] = useState<StockItem | null>(null)

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

  function handleWarehouseChange(value: string) {
    setWarehouseFilter(value)
    updateFilters({ warehouseId: value || undefined })
  }

  function handlePageChange(newPage: number) {
    updateFilters({ page: newPage })
  }

  function getVisiblePages(currentPage: number, totalPages: number): Array<number | '...'> {
    if (totalPages <= 7) return Array.from({ length: totalPages }, (_, i) => i + 1)
    if (currentPage <= 3) return [1, 2, 3, 4, '...', totalPages]
    if (currentPage >= totalPages - 2) return [1, '...', totalPages - 3, totalPages - 2, totalPages - 1, totalPages]
    return [1, '...', currentPage - 1, currentPage, currentPage + 1, '...', totalPages]
  }

  function clearFilters() {
    setSearchInput('')
    setWarehouseFilter('')
    setSearchParams(new URLSearchParams())
  }

  async function handleTransfer(data: { targetShelfId: string; qty: number; note?: string; serials?: string[] }) {
    if (!transferringItem) return
    await moveStock.mutateAsync({
      sourceStockItemId: transferringItem.id,
      targetShelfId: data.targetShelfId,
      qty: data.qty,
      note: data.note,
      serials: data.serials,
    })
    setTransferringItem(null)
  }

  const hasActiveFilters = warehouseFilter || searchInput

  if (error) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-red-800">
        خطا در دریافت اطلاعات: {error instanceof Error ? error.message : 'خطای ناشناخته'}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="انتقال کالا بین قفسه‌ها"
        description="انتقال کالاهای موجود در قفسه‌ها به قفسه‌های دیگر"
      />

      {/* Filters */}
      <div className="rounded-lg border border-slate-200 bg-white p-4">
        <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
          {/* Warehouse Filter */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">انبار</label>
            <select
              value={warehouseFilter}
              onChange={(e) => handleWarehouseChange(e.target.value)}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            >
              <option value="">همه انبارها</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>
          </div>

          {/* Search */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">جستجو</label>
            <input
              type="text"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder="نام محصول، SKU، لات..."
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
          </div>

          {/* Clear Filters */}
          {hasActiveFilters && (
            <div className="flex items-end">
              <button
                onClick={clearFilters}
                className="w-full rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50"
              >
                پاک کردن فیلترها
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Table */}
      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
        {isLoading ? (
          <TableSkeleton columns={7} rows={10} />
        ) : !data || data.items.length === 0 ? (
          <div className="py-12 text-center text-slate-500">
            کالایی با قفسه اختصاص داده شده یافت نشد.
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">محصول</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">SKU</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">لات</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">قفسه فعلی</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">موجودی کل</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">آزاد</th>
                    <th className="px-4 py-3 text-right text-xs font-medium uppercase text-slate-700">عملیات</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-200">
                  {data.items.map((item) => (
                    <tr key={item.id} className="hover:bg-slate-50">
                      <td className="px-4 py-3">
                        <div className="font-medium text-slate-900">
                          {item.productName || 'نامشخص'}
                          {item.variantValue && (
                            <span className="text-emerald-600"> - {item.variantValue}</span>
                          )}
                        </div>
                        {item.warehouseName && (
                          <div className="text-xs text-slate-500">{item.warehouseName}</div>
                        )}
                      </td>
                      <td className="px-4 py-3">
                        <span className="font-mono text-sm text-slate-600">{item.sku}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-slate-600">{item.lotNumber || '-'}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm font-medium text-slate-900">{item.shelfName || '-'}</span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm font-medium text-slate-900">
                          {item.onHand.toLocaleString('fa-IR')}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="text-sm text-slate-600">{item.available.toLocaleString('fa-IR')}</span>
                      </td>
                      <td className="px-4 py-3">
                        <button
                          onClick={() => setTransferringItem(item)}
                          className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-medium text-white transition-colors hover:bg-emerald-700"
                        >
                          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              d="M7.5 21 3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5"
                            />
                          </svg>
                          انتقال
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Pagination */}
            {data.totalPages > 1 && (
              <div className="border-t border-slate-200 bg-slate-50 px-4 py-3">
                <div className="flex items-center justify-between">
                  <div className="text-sm text-slate-700">
                    صفحه {data.page.toLocaleString('fa-IR')} از {data.totalPages.toLocaleString('fa-IR')} (
                    {data.totalCount.toLocaleString('fa-IR')} مورد)
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handlePageChange(data.page - 1)}
                      disabled={data.page <= 1}
                      className="rounded-lg border border-slate-300 bg-white px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50"
                    >
                      قبلی
                    </button>
                    <div className="flex items-center gap-1">
                      {getVisiblePages(data.page, data.totalPages).map((p, idx) =>
                        p === '...' ? (
                          <span key={`ellipsis-${idx}`} className="px-2 text-sm text-slate-500">
                            ...
                          </span>
                        ) : (
                          <button
                            key={p}
                            onClick={() => handlePageChange(p)}
                            disabled={p === data.page}
                            className={`min-w-[2.25rem] rounded-lg border px-3 py-1.5 text-sm font-medium transition-colors disabled:opacity-100 ${
                              p === data.page
                                ? 'border-emerald-600 bg-emerald-600 text-white'
                                : 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50'
                            }`}
                          >
                            {p.toLocaleString('fa-IR')}
                          </button>
                        )
                      )}
                    </div>
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
          </>
        )}
      </div>

      {/* Transfer Modal */}
      <TransferShelfModal
        isOpen={!!transferringItem}
        stockItem={transferringItem}
        onClose={() => setTransferringItem(null)}
        onSubmit={handleTransfer}
        isSubmitting={moveStock.isPending}
      />
    </div>
  )
}
