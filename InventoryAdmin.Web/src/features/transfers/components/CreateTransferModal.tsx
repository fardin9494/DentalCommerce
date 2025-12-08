import { useState } from 'react'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { Spinner } from '@/shared/components/Spinner'

interface CreateTransferModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { sourceWarehouseId: string; destinationWarehouseId: string; externalRef?: string }) => void
  isSubmitting?: boolean
}

export function CreateTransferModal({ isOpen, onClose, onSubmit, isSubmitting }: CreateTransferModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [sourceWarehouseId, setSourceWarehouseId] = useState('')
  const [destinationWarehouseId, setDestinationWarehouseId] = useState('')
  const [externalRef, setExternalRef] = useState('')

  if (!isOpen) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!sourceWarehouseId || !destinationWarehouseId) return
    if (sourceWarehouseId === destinationWarehouseId) {
      alert('انبار مبدا و مقصد نمی‌توانند یکسان باشند')
      return
    }
    onSubmit({
      sourceWarehouseId,
      destinationWarehouseId,
      externalRef: externalRef.trim() || undefined,
    })
  }

  function handleClose() {
    setSourceWarehouseId('')
    setDestinationWarehouseId('')
    setExternalRef('')
    onClose()
  }

  // Filter out selected source warehouse from destination options
  const availableDestinations = warehouses?.filter((w) => w.id !== sourceWarehouseId) || []

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">ایجاد انتقال جدید</h2>
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
          {/* Source Warehouse Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              انبار مبدا <span className="text-red-500">*</span>
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
                value={sourceWarehouseId}
                onChange={(e) => {
                  setSourceWarehouseId(e.target.value)
                  // Reset destination if it's the same as new source
                  if (e.target.value === destinationWarehouseId) {
                    setDestinationWarehouseId('')
                  }
                }}
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

          {/* Destination Warehouse Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              انبار مقصد <span className="text-red-500">*</span>
            </label>
            {loadingWarehouses ? (
              <div className="flex items-center justify-center py-4">
                <Spinner className="h-5 w-5 text-emerald-600" />
              </div>
            ) : !sourceWarehouseId ? (
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-sm text-slate-500">
                ابتدا انبار مبدا را انتخاب کنید
              </div>
            ) : availableDestinations.length === 0 ? (
              <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                انبار مقصد دیگری یافت نشد
              </div>
            ) : (
              <select
                value={destinationWarehouseId}
                onChange={(e) => setDestinationWarehouseId(e.target.value)}
                required
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              >
                <option value="">انتخاب کنید...</option>
                {availableDestinations.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* External Ref */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">مرجع خارجی (اختیاری)</label>
            <input
              type="text"
              value={externalRef}
              onChange={(e) => setExternalRef(e.target.value)}
              placeholder="مثال: PO-2024-001"
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
              disabled={isSubmitting || !sourceWarehouseId || !destinationWarehouseId || loadingWarehouses}
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
                  ایجاد انتقال
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

