import { useState } from 'react'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { Spinner } from '@/shared/components/Spinner'

interface CreateIssueModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { warehouseId?: string; externalRef?: string }) => Promise<void>
  isSubmitting: boolean
}

export function CreateIssueModal({ isOpen, onClose, onSubmit, isSubmitting }: CreateIssueModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [warehouseId, setWarehouseId] = useState('')
  const [externalRef, setExternalRef] = useState('')

  if (!isOpen) return null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    await onSubmit({
      warehouseId: warehouseId.trim() || undefined,
      externalRef: externalRef.trim() || undefined,
    })

    // Reset form on success
    setWarehouseId('')
    setExternalRef('')
  }

  function handleClose() {
    setWarehouseId('')
    setExternalRef('')
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
          <h2 className="text-xl font-bold text-slate-900">ایجاد خروجی جدید</h2>
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

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-5">
          {/* Warehouse Select (Optional) */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              انبار (اختیاری)
            </label>
            <p className="mb-2 text-xs text-slate-500">
              می‌توانید انبار را در هنگام تخصیص کالا انتخاب کنید
            </p>
            {loadingWarehouses ? (
              <div className="flex items-center justify-center py-4">
                <Spinner className="h-5 w-5 text-emerald-600" />
              </div>
            ) : !warehouses || warehouses.length === 0 ? (
              <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                هیچ انبار فعالی یافت نشد. می‌توانید در هنگام تخصیص کالا انبار را انتخاب کنید.
              </div>
            ) : (
              <select
                value={warehouseId}
                onChange={(e) => setWarehouseId(e.target.value)}
                disabled={isSubmitting}
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100 disabled:cursor-not-allowed"
              >
                <option value="">بدون انبار (انتخاب در تخصیص)</option>
                {warehouses.map((w) => (
                  <option key={w.id} value={w.id}>
                    {w.name} ({w.code})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* External Reference */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">شماره مرجع (اختیاری)</label>
            <input
              type="text"
              value={externalRef}
              onChange={(e) => setExternalRef(e.target.value)}
              placeholder="مثال: فاکتور-۱۲۳۴"
              disabled={isSubmitting}
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100 disabled:cursor-not-allowed"
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
              disabled={isSubmitting || loadingWarehouses}
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
                  ایجاد خروجی
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

