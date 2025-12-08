import { useState, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import type { TransferLine } from '../types'

interface EditTransferLineModalProps {
  isOpen: boolean
  line: TransferLine | null
  onClose: () => void
  onSubmit: (data: { qty?: number }) => void
  isSubmitting?: boolean
}

export function EditTransferLineModal({ isOpen, line, onClose, onSubmit, isSubmitting }: EditTransferLineModalProps) {
  const [qty, setQty] = useState('')

  // Initialize form when line changes
  useEffect(() => {
    if (line) {
      setQty(line.requestedQty.toString())
    }
  }, [line])

  if (!isOpen || !line) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    const qtyNum = parseFloat(qty)
    if (isNaN(qtyNum) || qtyNum <= 0) return

    // Check if new qty is less than allocated qty
    if (qtyNum < line.allocatedQty) {
      alert(`مقدار جدید نمی‌تواند کمتر از مقدار تخصیص داده شده (${line.allocatedQty}) باشد.`)
      return
    }

    onSubmit({
      qty: qtyNum,
    })
  }

  function handleClose() {
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">ویرایش خط انتقال</h2>
          <button
            onClick={handleClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Line Info */}
        <div className="mb-5 rounded-lg border border-slate-200 bg-slate-50 p-3">
          <div className="text-xs text-slate-500">ردیف {line.lineNo}</div>
          <div className="mt-1 font-mono text-sm text-slate-700">
            محصول: {line.productId.substring(0, 8)}...
            {line.variantId && (
              <span className="text-slate-500"> | واریانت: {line.variantId.substring(0, 8)}...</span>
            )}
          </div>
          {line.allocatedQty > 0 && (
            <div className="mt-2 text-xs text-orange-600">
              مقدار تخصیص داده شده: {line.allocatedQty.toLocaleString('fa-IR')}
            </div>
          )}
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {/* Quantity */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              تعداد <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              value={qty}
              onChange={(e) => setQty(e.target.value)}
              min={line.allocatedQty}
              step="0.01"
              required
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
            {line.allocatedQty > 0 && (
              <p className="mt-1 text-xs text-slate-500">
                حداقل: {line.allocatedQty.toLocaleString('fa-IR')} (مقدار تخصیص داده شده)
              </p>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end gap-3 pt-3">
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
              disabled={isSubmitting || !qty || parseFloat(qty) <= 0 || parseFloat(qty) < line.allocatedQty}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال ذخیره...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                  </svg>
                  ذخیره تغییرات
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

