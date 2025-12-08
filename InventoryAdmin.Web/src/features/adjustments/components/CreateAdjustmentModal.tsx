import { useState } from 'react'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { Spinner } from '@/shared/components/Spinner'
import { AdjustmentReasonLabels, type AdjustmentReason } from '../types'

interface CreateAdjustmentModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { warehouseId: string; reason: number; note?: string }) => void
  isSubmitting?: boolean
}

const reasons: { value: number; label: string; key: AdjustmentReason }[] = [
  { value: 1, label: AdjustmentReasonLabels.InitialBalance, key: 'InitialBalance' },
  { value: 2, label: AdjustmentReasonLabels.Damage, key: 'Damage' },
  { value: 3, label: AdjustmentReasonLabels.Expired, key: 'Expired' },
  { value: 4, label: AdjustmentReasonLabels.Found, key: 'Found' },
  { value: 5, label: AdjustmentReasonLabels.Shrinkage, key: 'Shrinkage' },
  { value: 6, label: AdjustmentReasonLabels.Correction, key: 'Correction' },
  { value: 99, label: AdjustmentReasonLabels.Other, key: 'Other' },
]

export function CreateAdjustmentModal({
  isOpen,
  onClose,
  onSubmit,
  isSubmitting,
}: CreateAdjustmentModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [warehouseId, setWarehouseId] = useState('')
  const [reason, setReason] = useState(6) // Default to Correction
  const [note, setNote] = useState('')

  if (!isOpen) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!warehouseId) return
    onSubmit({
      warehouseId,
      reason,
      note: note.trim() || undefined,
    })
  }

  function handleClose() {
    setWarehouseId('')
    setReason(6)
    setNote('')
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
          <h2 className="text-xl font-bold text-slate-900">ایجاد اصلاح موجودی جدید</h2>
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
          {/* Warehouse Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              انبار <span className="text-red-500">*</span>
            </label>
            {loadingWarehouses ? (
              <div className="flex items-center justify-center py-4">
                <Spinner className="h-5 w-5 text-emerald-600" />
              </div>
            ) : !warehouses || warehouses.length === 0 ? (
              <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                هیچ انبار فعالی یافت نشد. لطفاً ابتدا یک انبار ایجاد کنید.
              </div>
            ) : (
              <select
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
                required
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              >
                <option value="">انتخاب کنید...</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Reason Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              دلیل اصلاح <span className="text-red-500">*</span>
            </label>
            <div className="grid grid-cols-2 gap-2">
              {reasons.map((r) => (
                <button
                  key={r.value}
                  type="button"
                  onClick={() => setReason(r.value)}
                  className={`rounded-lg border-2 px-3 py-2 text-xs font-medium transition-all ${
                    reason === r.value
                      ? 'border-emerald-500 bg-emerald-50 text-emerald-700'
                      : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                  }`}
                >
                  {r.label}
                </button>
              ))}
            </div>
          </div>

          {/* Note */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">یادداشت (اختیاری)</label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="توضیحات اصلاح موجودی..."
              rows={3}
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
              disabled={isSubmitting || !warehouseId || loadingWarehouses}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال ایجاد...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                  ایجاد اصلاح
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

