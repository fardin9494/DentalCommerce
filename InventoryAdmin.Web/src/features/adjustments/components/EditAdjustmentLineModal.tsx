import { useState, useEffect } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useProductNames } from '@/shared/hooks/useProductNames'
import type { AdjustmentLine } from '../types'

interface EditAdjustmentLineModalProps {
  isOpen: boolean
  line: AdjustmentLine
  onClose: () => void
  onSubmit: (data: { qtyDelta: number }) => void
  isSubmitting?: boolean
}

export function EditAdjustmentLineModal({
  isOpen,
  line,
  onClose,
  onSubmit,
  isSubmitting,
}: EditAdjustmentLineModalProps) {
  const [qtyDelta, setQtyDelta] = useState('0')
  
  const { getProductName, getVariantName } = useProductNames([line.productId])

  useEffect(() => {
    if (line) {
      setQtyDelta(line.qtyDelta.toString())
    }
  }, [line])

  if (!isOpen) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const qtyDeltaNum = parseFloat(qtyDelta)
    if (isNaN(qtyDeltaNum) || qtyDeltaNum === 0) return
    onSubmit({ qtyDelta: qtyDeltaNum })
  }

  function handleClose() {
    setQtyDelta(line.qtyDelta.toString())
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
          <h2 className="text-xl font-bold text-slate-900">ویرایش خط اصلاح</h2>
          <button
            onClick={handleClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          {/* Line Info */}
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
            <div className="text-sm font-medium text-slate-700">ردیف: {line.lineNo}</div>
            <div className="mt-1 text-sm text-slate-700">
              <div className="font-medium text-slate-900">محصول: {getProductName(line.productId)}</div>
              {line.variantId && (
                <div className="mt-0.5 text-sm text-emerald-600">
                  واریانت: {getVariantName(line.variantId) || line.variantId.substring(0, 8) + '...'}
                </div>
              )}
            </div>
          </div>

          {/* Qty Delta */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              تغییر موجودی <span className="text-red-500">*</span>
            </label>
            <div className="space-y-2">
              <input
                type="number"
                value={qtyDelta}
                onChange={(e) => setQtyDelta(e.target.value)}
                step="0.01"
                required
                placeholder="مثبت برای افزایش، منفی برای کاهش"
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              />
              <p className="text-xs text-slate-500">
                عدد مثبت برای افزایش موجودی، عدد منفی برای کاهش موجودی
              </p>
            </div>
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
              disabled={isSubmitting || parseFloat(qtyDelta) === 0}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال به‌روزرسانی...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z"
                    />
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

