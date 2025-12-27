import { useState, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useStockItems } from '@/features/stock-items/queries'
import { StockItemSerialsModal } from '@/features/stock-items/components/StockItemSerialsModal'
import type { StockItem } from '@/features/stock-items/api'
import type { Shelf } from '../api'

interface ShelfProductsModalProps {
  isOpen: boolean
  shelf: Shelf | null
  onClose: () => void
}

export function ShelfProductsModal({ isOpen, shelf, onClose }: ShelfProductsModalProps) {
  const [page, setPage] = useState(1)
  const [serialsItem, setSerialsItem] = useState<StockItem | null>(null)
  const pageSize = 20

  const { data, isLoading, error } = useStockItems(
    {
      shelfId: shelf?.id,
      hasStock: true,
      page,
      pageSize,
    },
    isOpen && !!shelf
  )

  // Reset page when shelf changes
  useEffect(() => {
    if (shelf) {
      setPage(1)
    }
  }, [shelf?.id])

  if (!isOpen || !shelf) return null

  function formatDate(dateStr: string | null | undefined) {
    if (!dateStr) return '-'
    try {
      return new Date(dateStr).toLocaleDateString('fa-IR', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
      })
    } catch {
      return dateStr
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-5xl max-h-[90vh] rounded-xl bg-white shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-900">محصولات قفسه "{shelf.name}"</h3>
            <p className="mt-1 text-sm text-slate-500">
              {data?.totalCount ? `${data.totalCount.toLocaleString('fa-IR')} محصول` : 'در حال بارگذاری...'}
            </p>
          </div>
          <button
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-6 py-4">
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
              <p className="text-slate-600">محصولی در این قفسه یافت نشد</p>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-slate-200 bg-slate-50 text-right">
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">نام محصول</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">واریانت</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">شماره لات</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تاریخ انقضا</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">تعداد</th>
                    <th className="whitespace-nowrap px-4 py-3 font-semibold text-slate-600">Serials</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.items.map((item) => (
                    <tr key={item.id} className="transition-colors hover:bg-slate-50">
                      <td className="whitespace-nowrap px-4 py-3">
                        <div className="font-medium text-slate-900">{item.productName || item.sku}</div>
                        <div className="mt-0.5 text-xs text-slate-500 font-mono">{item.sku}</div>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        {item.variantValue ? (
                          <span className="text-sm text-emerald-600">{item.variantValue}</span>
                        ) : (
                          <span className="text-slate-400">-</span>
                        )}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">{item.lotNumber || '-'}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-slate-600">
                        {item.expiryDate ? formatDate(item.expiryDate) : '-'}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                        <span className="font-medium text-slate-900">{item.onHand.toLocaleString('fa-IR')}</span>
                      </td>
                      <td className="whitespace-nowrap px-4 py-3">
                         {item.availableSerialsCount > 0 ? (
                           <button
                             onClick={() => setSerialsItem(item)}
                             className="inline-flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-2.5 py-1 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50"
                           >
                             View ({item.availableSerialsCount.toLocaleString('fa-IR')})
                           </button>
                         ) : (
                           <span className="text-slate-400">-</span>
                         )}
                       </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* Footer with Pagination */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-slate-200 px-6 py-4">
            <div className="text-sm text-slate-600">
              صفحه {page.toLocaleString('fa-IR')} از {data.totalPages.toLocaleString('fa-IR')}
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                قبلی
              </button>
              <button
                onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                disabled={page === data.totalPages}
                className="rounded-lg border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                بعدی
              </button>
            </div>
          </div>
        )}

        {/* Close Button */}
        <div className="border-t border-slate-200 px-6 py-4">
          <button
            onClick={onClose}
            className="w-full rounded-lg bg-slate-100 px-4 py-2.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-200"
          >
            بستن
          </button>
        </div>
        <StockItemSerialsModal
          isOpen={!!serialsItem}
          stockItem={serialsItem}
          onClose={() => setSerialsItem(null)}
        />
      </div>
    </div>
  )
}


