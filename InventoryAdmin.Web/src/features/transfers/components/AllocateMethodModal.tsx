import { Spinner } from '@/shared/components/Spinner'

interface AllocateMethodModalProps {
  isOpen: boolean
  onClose: () => void
  onSelectFefo: () => void
  onSelectFifo: () => void
  onSelectLifo: () => void
  isAllocating: boolean
}

export function AllocateMethodModal({
  isOpen,
  onClose,
  onSelectFefo,
  onSelectFifo,
  onSelectLifo,
  isAllocating,
}: AllocateMethodModalProps) {
  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">انتخاب روش تخصیص موجودی</h2>
          <button
            onClick={onClose}
            disabled={isAllocating}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 disabled:opacity-50"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Options */}
        <div className="space-y-3">
          <button
            onClick={() => {
              onSelectFefo()
              onClose()
            }}
            disabled={isAllocating}
            className="w-full rounded-lg border-2 border-blue-200 bg-blue-50 p-4 text-right transition-colors hover:border-blue-300 hover:bg-blue-100 disabled:opacity-50"
          >
            <div className="font-semibold text-blue-900">FEFO (First Expiry First Out)</div>
            <div className="mt-1 text-sm text-blue-700">اول انقضا، اول خروج</div>
            <div className="mt-1 text-xs text-blue-600">مناسب برای محصولات با تاریخ انقضا (دارو، مواد غذایی)</div>
          </button>

          <button
            onClick={() => {
              onSelectFifo()
              onClose()
            }}
            disabled={isAllocating}
            className="w-full rounded-lg border-2 border-emerald-200 bg-emerald-50 p-4 text-right transition-colors hover:border-emerald-300 hover:bg-emerald-100 disabled:opacity-50"
          >
            <div className="font-semibold text-emerald-900">FIFO (First In First Out)</div>
            <div className="mt-1 text-sm text-emerald-700">اول ورود، اول خروج</div>
            <div className="mt-1 text-xs text-emerald-600">مناسب برای مدیریت موجودی استاندارد</div>
          </button>

          <button
            onClick={() => {
              onSelectLifo()
              onClose()
            }}
            disabled={isAllocating}
            className="w-full rounded-lg border-2 border-purple-200 bg-purple-50 p-4 text-right transition-colors hover:border-purple-300 hover:bg-purple-100 disabled:opacity-50"
          >
            <div className="font-semibold text-purple-900">LIFO (Last In First Out)</div>
            <div className="mt-1 text-sm text-purple-700">آخر ورود، اول خروج</div>
            <div className="mt-1 text-xs text-purple-600">مناسب برای محصولات بدون تاریخ انقضا</div>
          </button>
        </div>

        {/* Loading Overlay */}
        {isAllocating && (
          <div className="absolute inset-0 flex items-center justify-center rounded-2xl bg-white/80 backdrop-blur-sm">
            <Spinner className="h-8 w-8 text-emerald-600" />
          </div>
        )}
      </div>
    </div>
  )
}

