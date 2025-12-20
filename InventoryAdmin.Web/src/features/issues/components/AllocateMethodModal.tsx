import { useEffect, useState } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'

type AllocationMethod = 'fefo' | 'fifo' | 'lifo'

interface AllocateMethodModalProps {
  isOpen: boolean
  onClose: () => void
  onSelect: (method: AllocationMethod, preferredWarehouseId?: string) => void
  lineNo: number
  isAllocating?: boolean
  defaultWarehouseId?: string
}

export function AllocateMethodModal({ 
  isOpen, 
  onClose, 
  onSelect, 
  lineNo, 
  isAllocating = false,
  defaultWarehouseId 
}: AllocateMethodModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string>('')
  const isLockedToDefault = !!defaultWarehouseId

  useEffect(() => {
    if (!isOpen) return
    setSelectedWarehouseId(defaultWarehouseId || '')
  }, [isOpen, defaultWarehouseId])

  if (!isOpen) return null

  function handleSelect(method: AllocationMethod) {
    const warehouseId = selectedWarehouseId || undefined
    onSelect(method, warehouseId)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">انتخاب روش تخصیص برای خط {lineNo}</h2>
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

        {/* Warehouse Selection (Optional) */}
        <div className="mb-4">
          <label className="mb-2 block text-sm font-medium text-slate-700">
            انتخاب انبار (اختیاری)
          </label>
          {loadingWarehouses ? (
            <div className="flex items-center justify-center py-2">
              <Spinner className="h-4 w-4 text-emerald-600" />
            </div>
          ) : (
            <select
              value={selectedWarehouseId}
              onChange={(e) => setSelectedWarehouseId(e.target.value)}
              disabled={isAllocating || isLockedToDefault}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20 disabled:bg-slate-100 disabled:cursor-not-allowed"
            >
              <option value="">همه انبارها (انتخاب خودکار)</option>
              {warehouses?.map((w) => (
                <option key={w.id} value={w.id}>
                  {w.name} ({w.code})
                </option>
              ))}
            </select>
          )}
          <p className="mt-1 text-xs text-slate-500">
            اگر انباری انتخاب نشود، سیستم به صورت خودکار از تمام انبارها با متد انتخابی جستجو می‌کند
          </p>
        </div>

        {/* Options */}
        <div className="space-y-3">
          <button
            onClick={() => {
              handleSelect('fefo')
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
              handleSelect('fifo')
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
              handleSelect('lifo')
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
