import { Spinner } from '@/shared/components/Spinner'
import { useStockItemSerials } from '../queries'
import type { StockItem } from '../api'

interface StockItemSerialsModalProps {
  isOpen: boolean
  stockItem: StockItem | null
  onClose: () => void
}

export function StockItemSerialsModal({ isOpen, stockItem, onClose }: StockItemSerialsModalProps) {
  const { data, isLoading, error } = useStockItemSerials(stockItem?.id, 'Available', isOpen && !!stockItem)

  if (!isOpen || !stockItem) return null

  const title = stockItem.productName || stockItem.sku
  const serials = data || []

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-3xl max-h-[90vh] rounded-xl bg-white shadow-xl flex flex-col">
        <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <div>
            <h3 className="text-lg font-semibold text-slate-900">سریال‌ها</h3>
            <p className="mt-1 text-sm text-slate-500">
              {title} - {stockItem.sku}
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

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {isLoading ? (
            <div className="flex items-center justify-center py-20">
              <Spinner className="h-8 w-8 text-emerald-600" />
            </div>
          ) : error ? (
            <div className="rounded-xl border border-red-200 bg-red-50 p-6 text-center">
              <p className="text-sm text-red-700">{(error as Error).message || 'خطا در دریافت سریال‌ها'}</p>
            </div>
          ) : serials.length === 0 ? (
            <div className="rounded-xl border border-slate-200 bg-slate-50 p-6 text-center">
              <p className="text-sm text-slate-600">سریال موجود یافت نشد.</p>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="text-sm text-slate-600">
                تعداد: <span className="font-medium text-slate-900">{serials.length.toLocaleString('fa-IR')}</span>
              </div>
              <div className="grid gap-2 sm:grid-cols-2">
                {serials.map((serial) => (
                  <div key={serial.serialNumber} className="rounded-lg border border-slate-200 bg-white px-3 py-2">
                    <div className="font-mono text-sm text-slate-900">{serial.serialNumber}</div>
                    <div className="text-xs text-slate-500">{serial.status}</div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="border-t border-slate-200 px-6 py-4">
          <button
            onClick={onClose}
            className="w-full rounded-lg bg-slate-100 px-4 py-2.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-200"
          >
            بستن
          </button>
        </div>
      </div>
    </div>
  )
}

