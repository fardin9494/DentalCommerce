import { useEffect, useMemo, useState } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useActiveWarehouses } from '@/shared/hooks/useWarehouses'
import { useIssueLineAvailableSerials } from '../queries'

type AllocationMethod = 'fefo' | 'fifo' | 'lifo'
type AllocateTab = 'system' | 'serials'

interface AllocateMethodModalProps {
  isOpen: boolean
  issueId: string
  lineId: string
  lineNo: number
  requestedQty: number
  onClose: () => void
  onSelect: (method: AllocationMethod, preferredWarehouseId?: string) => void
  onSelectSerials: (serials: string[], preferredWarehouseId?: string) => void
  isAllocating?: boolean
  defaultWarehouseId?: string
  initialTab?: AllocateTab
}

export function AllocateMethodModal({
  isOpen,
  issueId,
  lineId,
  lineNo,
  requestedQty,
  onClose,
  onSelect,
  onSelectSerials,
  isAllocating = false,
  defaultWarehouseId,
  initialTab = 'system',
}: AllocateMethodModalProps) {
  const { data: warehouses, isLoading: loadingWarehouses } = useActiveWarehouses()
  const [selectedWarehouseId, setSelectedWarehouseId] = useState<string>('')
  const [activeTab, setActiveTab] = useState<AllocateTab>('system')
  const [serialSearch, setSerialSearch] = useState('')
  const [selectedSerials, setSelectedSerials] = useState<string[]>([])
  const isLockedToDefault = !!defaultWarehouseId

  const requiredQty = Number.isInteger(requestedQty) ? Math.trunc(requestedQty) : null

  useEffect(() => {
    if (!isOpen) return
    setSelectedWarehouseId(defaultWarehouseId || '')
    setActiveTab(initialTab)
    setSerialSearch('')
    setSelectedSerials([])
  }, [isOpen, defaultWarehouseId, initialTab])

  useEffect(() => {
    if (!isOpen) return
    if (activeTab === 'serials') {
      setSelectedSerials([])
    }
  }, [activeTab, selectedWarehouseId, lineId, isOpen])

  const serialsEnabled = isOpen && activeTab === 'serials' && requiredQty !== null
  const { data: availableSerials, isLoading: loadingSerials, error: serialsError } = useIssueLineAvailableSerials(
    issueId,
    lineId,
    selectedWarehouseId || undefined,
    serialsEnabled
  )

  useEffect(() => {
    if (!availableSerials || availableSerials.length === 0) return
    setSelectedSerials((prev) => prev.filter((s) => availableSerials.some((x) => x.serialNumber === s)))
  }, [availableSerials])

  const filteredSerials = useMemo(() => {
    if (!availableSerials) return []
    const term = serialSearch.trim().toLowerCase()
    if (!term) return availableSerials
    return availableSerials.filter((serial) => serial.serialNumber.toLowerCase().includes(term))
  }, [availableSerials, serialSearch])

  function handleSelect(method: AllocationMethod) {
    const warehouseId = selectedWarehouseId || undefined
    onSelect(method, warehouseId)
  }

  function toggleSerial(serialNumber: string) {
    setSelectedSerials((prev) => {
      if (prev.includes(serialNumber)) {
        return prev.filter((s) => s !== serialNumber)
      }
      return [...prev, serialNumber]
    })
  }

  const selectionLimitReached = requiredQty !== null && selectedSerials.length >= requiredQty
  const canSubmitSerials = requiredQty !== null && selectedSerials.length === requiredQty

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={onClose} />

      <div className="relative w-full max-w-2xl rounded-2xl bg-white p-6 shadow-2xl">
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

        <div className="mb-4">
          <label className="mb-2 block text-sm font-medium text-slate-700">انتخاب انبار (اختیاری)</label>
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
            اگر انباری انتخاب نشود، سیستم به صورت خودکار از تمام انبارها با روش انتخابی استفاده می‌کند.
          </p>
        </div>

        <div className="mb-4 flex gap-2">
          <button
            type="button"
            onClick={() => setActiveTab('system')}
            className={`flex-1 rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === 'system'
                ? 'border-emerald-500 bg-emerald-50 text-emerald-700'
                : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
            }`}
          >
            تخصیص سیستمی
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('serials')}
            className={`flex-1 rounded-lg border px-4 py-2 text-sm font-medium transition-colors ${
              activeTab === 'serials'
                ? 'border-blue-500 bg-blue-50 text-blue-700'
                : 'border-slate-200 bg-white text-slate-600 hover:bg-slate-50'
            }`}
          >
            انتخاب سریال‌ها
          </button>
        </div>

        {activeTab === 'system' ? (
          <div className="space-y-3">
            <button
              onClick={() => {
                handleSelect('fefo')
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
              }}
              disabled={isAllocating}
              className="w-full rounded-lg border-2 border-purple-200 bg-purple-50 p-4 text-right transition-colors hover:border-purple-300 hover:bg-purple-100 disabled:opacity-50"
            >
              <div className="font-semibold text-purple-900">LIFO (Last In First Out)</div>
              <div className="mt-1 text-sm text-purple-700">آخر ورود، اول خروج</div>
              <div className="mt-1 text-xs text-purple-600">مناسب برای محصولات بدون تاریخ انقضا</div>
            </button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-700">
              <div className="flex items-center justify-between">
                <span>تعداد مورد نیاز:</span>
                <span className="font-semibold">{requiredQty?.toLocaleString('fa-IR') ?? '-'}</span>
              </div>
              <div className="mt-1 flex items-center justify-between">
                <span>انتخاب شده:</span>
                <span className={`font-semibold ${canSubmitSerials ? 'text-emerald-600' : 'text-slate-700'}`}>
                  {selectedSerials.length.toLocaleString('fa-IR')}
                </span>
              </div>
              {requiredQty === null && (
                <p className="mt-2 text-xs text-amber-600">
                  این خط مقدار اعشاری دارد و امکان تخصیص سریال ندارد.
                </p>
              )}
            </div>

            {requiredQty !== null && (
              <>
                <div className="flex items-center gap-2">
                  <input
                    value={serialSearch}
                    onChange={(e) => setSerialSearch(e.target.value)}
                    placeholder="جستجو در سریال‌ها..."
                    className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm transition-colors focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20"
                  />
                  <button
                    type="button"
                    onClick={() => setSelectedSerials([])}
                    className="rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 transition-colors hover:bg-slate-50"
                  >
                    پاک کردن
                  </button>
                </div>

                {loadingSerials ? (
                  <div className="flex items-center justify-center py-6">
                    <Spinner className="h-5 w-5 text-emerald-600" />
                  </div>
                ) : serialsError ? (
                  <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
                    {(serialsError as Error).message || 'خطا در دریافت سریال‌ها'}
                  </div>
                ) : filteredSerials.length === 0 ? (
                  <div className="rounded-lg border border-slate-200 bg-white p-4 text-sm text-slate-600">
                    سریال در دسترس یافت نشد.
                  </div>
                ) : (
                  <div className="max-h-64 overflow-y-auto rounded-lg border border-slate-200 bg-white">
                    <div className="divide-y divide-slate-100">
                      {filteredSerials.map((serial) => {
                        const isChecked = selectedSerials.includes(serial.serialNumber)
                        const isDisabled = !isChecked && selectionLimitReached
                        return (
                          <label key={serial.serialNumber} className="flex items-start gap-3 px-4 py-2 text-sm">
                            <input
                              type="checkbox"
                              checked={isChecked}
                              disabled={isAllocating || isDisabled}
                              onChange={() => toggleSerial(serial.serialNumber)}
                              className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                            />
                            <div className="flex-1">
                              <div className="font-mono text-slate-900">{serial.serialNumber}</div>
                              <div className="mt-0.5 text-xs text-slate-500">
                                {[serial.sku, serial.lotNumber, serial.shelfName || serial.warehouseName]
                                  .filter(Boolean)
                                  .join(' - ')}
                              </div>
                            </div>
                          </label>
                        )
                      })}
                    </div>
                  </div>
                )}

                <button
                  type="button"
                  onClick={() => {
                    onSelectSerials(selectedSerials, selectedWarehouseId || undefined)
                  }}
                  disabled={isAllocating || !canSubmitSerials}
                  className="w-full rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-medium text-white transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  تخصیص سریال‌ها
                </button>
              </>
            )}
          </div>
        )}

        {isAllocating && (
          <div className="absolute inset-0 flex items-center justify-center rounded-2xl bg-white/80 backdrop-blur-sm">
            <Spinner className="h-8 w-8 text-emerald-600" />
          </div>
        )}
      </div>
    </div>
  )
}
