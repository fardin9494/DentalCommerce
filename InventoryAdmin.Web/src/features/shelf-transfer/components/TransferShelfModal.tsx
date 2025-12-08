import { useState, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useActiveShelves } from '../../shelves/queries'
import type { StockItem } from '../../stock-items/api'

interface TransferShelfModalProps {
  isOpen: boolean
  stockItem: StockItem | null
  onClose: () => void
  onSubmit: (data: { targetShelfId: string; qty: number; note?: string }) => void
  isSubmitting?: boolean
}

export function TransferShelfModal({ isOpen, stockItem, onClose, onSubmit, isSubmitting }: TransferShelfModalProps) {
  const { data: shelves, isLoading: loadingShelves } = useActiveShelves(stockItem?.warehouseId)
  const [targetShelfId, setTargetShelfId] = useState('')
  const [qty, setQty] = useState('')
  const [note, setNote] = useState('')

  // Initialize form when stockItem changes
  useEffect(() => {
    if (stockItem) {
      setQty(stockItem.available.toString())
      setNote('')
      setTargetShelfId('')
    }
  }, [stockItem])

  if (!isOpen || !stockItem) return null

  // Filter out the current shelf from the list
  const availableShelves = shelves?.filter(s => s.id !== stockItem.shelfId) || []

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!targetShelfId || !qty) return

    const qtyNum = parseFloat(qty)
    if (isNaN(qtyNum) || qtyNum <= 0 || qtyNum > stockItem.onHand) return

    onSubmit({
      targetShelfId,
      qty: qtyNum,
      note: note.trim() || undefined,
    })
  }

  function handleClose() {
    onClose()
  }

  const maxQty = stockItem.onHand

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">انتقال به قفسه دیگر</h2>
          <button
            onClick={handleClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Stock Item Info */}
        <div className="mb-5 rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <div className="text-sm font-medium text-emerald-800">کالای انتخابی</div>
          <div className="mt-1 font-medium text-slate-900">
            {stockItem.productName || 'نامشخص'}
            {stockItem.variantValue && <span className="text-emerald-600"> - {stockItem.variantValue}</span>}
          </div>
          <div className="mt-0.5 flex items-center gap-4 text-xs text-slate-600">
            <span className="font-mono">SKU: {stockItem.sku}</span>
            <span>•</span>
            <span>موجودی کل: {stockItem.onHand.toLocaleString('fa-IR')}</span>
            <span>•</span>
            <span>آزاد: {stockItem.available.toLocaleString('fa-IR')}</span>
          </div>
          {stockItem.lotNumber && (
            <div className="mt-1 text-xs text-slate-500">لات: {stockItem.lotNumber}</div>
          )}
          {stockItem.shelfName && (
            <div className="mt-1 text-xs text-slate-500">
              قفسه فعلی: <span className="font-medium">{stockItem.shelfName}</span>
            </div>
          )}
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          {/* Current Shelf (Read-only) */}
          {stockItem.shelfName && (
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">قفسه فعلی</label>
              <input
                type="text"
                value={stockItem.shelfName}
                disabled
                className="w-full rounded-lg border border-slate-300 bg-slate-50 px-4 py-2.5 text-sm text-slate-600"
              />
            </div>
          )}

          {/* Target Shelf Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              قفسه مقصد <span className="text-red-500">*</span>
            </label>
            {loadingShelves ? (
              <div className="flex items-center justify-center py-4">
                <Spinner className="h-5 w-5 text-emerald-600" />
              </div>
            ) : availableShelves.length === 0 ? (
              <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                هیچ قفسه دیگری در این انبار یافت نشد.
              </div>
            ) : (
              <select
                value={targetShelfId}
                onChange={(e) => setTargetShelfId(e.target.value)}
                required
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              >
                <option value="">انتخاب کنید...</option>
                {availableShelves.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name} {s.description && `- ${s.description}`}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Quantity */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              تعداد <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              value={qty}
              onChange={(e) => setQty(e.target.value)}
              min="0.01"
              max={maxQty}
              step="0.01"
              required
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
            <p className="mt-1 text-xs text-slate-500">
              حداکثر: {maxQty.toLocaleString('fa-IR')} (موجودی کل)
            </p>
          </div>

          {/* Note */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">یادداشت (اختیاری)</label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="یادداشت انتقال..."
              rows={2}
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting}
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:opacity-50"
            >
              انصراف
            </button>
            <button
              type="submit"
              disabled={
                isSubmitting ||
                !targetShelfId ||
                !qty ||
                parseFloat(qty) <= 0 ||
                parseFloat(qty) > maxQty ||
                loadingShelves ||
                availableShelves.length === 0
              }
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال انتقال...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M7.5 21 3 16.5m0 0L7.5 12M3 16.5h13.5m0-13.5L21 7.5m0 0L16.5 12M21 7.5H7.5"
                    />
                  </svg>
                  انتقال
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

