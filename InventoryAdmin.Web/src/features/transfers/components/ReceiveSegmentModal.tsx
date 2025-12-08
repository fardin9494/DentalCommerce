import { useState, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import type { TransferSegment } from '../types'

interface ReceiveSegmentModalProps {
  isOpen: boolean
  segment: TransferSegment | null
  onClose: () => void
  onSubmit: (qty: number) => Promise<void>
  isSubmitting?: boolean
}

export function ReceiveSegmentModal({
  isOpen,
  segment,
  onClose,
  onSubmit,
  isSubmitting = false,
}: ReceiveSegmentModalProps) {
  const [qty, setQty] = useState('')

  useEffect(() => {
    if (segment && isOpen) {
      setQty(segment.remainingToReceive.toString())
    }
  }, [segment, isOpen])

  if (!isOpen || !segment) return null

  const maxQty = segment.remainingToReceive
  const qtyNum = parseFloat(qty) || 0
  const isValid = qtyNum > 0 && qtyNum <= maxQty

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!isValid) return

    try {
      await onSubmit(qtyNum)
      onClose()
    } catch {
      // Error handled by parent
    }
  }

  function handleClose() {
    if (!isSubmitting) {
      onClose()
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">دریافت بخش انتقال</h2>
          <button
            onClick={handleClose}
            disabled={isSubmitting}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 disabled:opacity-50"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Segment Info */}
        <div className="mb-5 rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <div className="text-sm font-medium text-emerald-800">اطلاعات بخش</div>
          <div className="mt-2 space-y-1 text-sm">
            <div className="flex items-center justify-between">
              <span className="text-slate-600">مقدار کل:</span>
              <span className="font-semibold text-slate-900">{segment.qty.toLocaleString('fa-IR')}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-slate-600">دریافت شده:</span>
              <span className="font-semibold text-emerald-700">
                {segment.receivedQty.toLocaleString('fa-IR')}
              </span>
            </div>
            <div className="flex items-center justify-between border-t border-emerald-200 pt-1">
              <span className="font-medium text-slate-700">باقی‌مانده:</span>
              <span className="font-bold text-emerald-800">{maxQty.toLocaleString('fa-IR')}</span>
            </div>
          </div>
          {segment.sku && (
            <div className="mt-2 text-xs text-slate-500">
              <span className="font-mono">SKU: {segment.sku}</span>
            </div>
          )}
          {segment.lotNumber && (
            <div className="mt-1 text-xs text-slate-500">لات: {segment.lotNumber}</div>
          )}
          {segment.expiryDate && (
            <div className="mt-1 text-xs text-slate-500">
              انقضا: {new Date(segment.expiryDate).toLocaleDateString('fa-IR')}
            </div>
          )}
          {segment.shelfName && (
            <div className="mt-1">
              <span className="inline-flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-medium text-emerald-700">
                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z"
                  />
                </svg>
                {segment.shelfName}
              </span>
            </div>
          )}
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5">
          <div>
            <label htmlFor="qty" className="mb-2 block text-sm font-medium text-slate-700">
              مقدار دریافت <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              id="qty"
              value={qty}
              onChange={(e) => setQty(e.target.value)}
              min="0.01"
              max={maxQty}
              step="0.01"
              required
              disabled={isSubmitting}
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:opacity-50"
            />
            <p className="mt-1 text-xs text-slate-500">
              حداکثر: {maxQty.toLocaleString('fa-IR')} (باقی‌مانده)
            </p>
            {qtyNum > maxQty && (
              <p className="mt-1 text-xs text-red-600">مقدار نمی‌تواند بیشتر از باقی‌مانده باشد</p>
            )}
            {qtyNum <= 0 && qty !== '' && (
              <p className="mt-1 text-xs text-red-600">مقدار باید بیشتر از صفر باشد</p>
            )}
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
              disabled={!isValid || isSubmitting}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال دریافت...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="m4.5 12.75 6 6 9-13.5"
                    />
                  </svg>
                  ثبت دریافت
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

