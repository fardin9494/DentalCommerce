import { useState, useEffect, useMemo } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useActiveShelves } from '../../shelves/queries'
import { useStockItemSerials } from '../../stock-items/queries'
import type { UnassignedStockItem } from '../api'

interface AssignShelfModalProps {
  isOpen: boolean
  stockItem: UnassignedStockItem | null
  onClose: () => void
  onSubmit: (data: { shelfId: string; qty: number; note?: string; serials?: string[] }) => void
  isSubmitting?: boolean
}

export function AssignShelfModal({ isOpen, stockItem, onClose, onSubmit, isSubmitting }: AssignShelfModalProps) {
  const { data: shelves, isLoading: loadingShelves } = useActiveShelves(stockItem?.warehouseId)
  const [shelfId, setShelfId] = useState('')
  const [qty, setQty] = useState('')
  const [note, setNote] = useState('')
  const [serialSearch, setSerialSearch] = useState('')
  const [selectedSerials, setSelectedSerials] = useState<string[]>([])
  const serialStatus = stockItem?.blocked > 0 ? 'AwaitingShelving' : 'Available'
  const serialsEnabled = isOpen && !!stockItem
  const { data: serials, isLoading: loadingSerials, error: serialsError } = useStockItemSerials(
    stockItem?.id,
    serialStatus,
    serialsEnabled
  )

  // Initialize form when stockItem changes
  useEffect(() => {
    if (stockItem && isOpen) {
      const qtyToShow = stockItem.blocked > 0 ? stockItem.blocked : stockItem.available
      setQty(qtyToShow.toString())
      setNote('')
      setShelfId('')
      setSerialSearch('')
      setSelectedSerials([])
    }
  }, [stockItem, isOpen])

  useEffect(() => {
    if (!serials || serials.length === 0) return
    setSelectedSerials((prev) => prev.filter((s) => serials.some((x) => x.serialNumber === s)))
  }, [serials])

  const filteredSerials = useMemo(() => {
    if (!serials) return []
    const term = serialSearch.trim().toLowerCase()
    if (!term) return serials
    return serials.filter((serial) => serial.serialNumber.toLowerCase().includes(term))
  }, [serials, serialSearch])

  if (!isOpen || !stockItem) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!shelfId) return

    const qtyNum = hasSerials ? selectedSerials.length : parseFloat(qty)
    if (hasSerials) {
      if (selectedSerials.length === 0) return
    } else {
      if (!qty || isNaN(qtyNum) || qtyNum <= 0 || qtyNum > maxQty) return
    }

    onSubmit({
      shelfId,
      qty: qtyNum,
      note: note.trim() || undefined,
      serials: hasSerials ? selectedSerials : undefined,
    })
  }

  function handleClose() {
    onClose()
  }

  function toggleSerial(serialNumber: string) {
    setSelectedSerials((prev) => {
      if (prev.includes(serialNumber)) {
        return prev.filter((s) => s !== serialNumber)
      }
      return [...prev, serialNumber]
    })
  }

  function selectAllSerials() {
    if (serialLimit === 0 || filteredSerials.length === 0) return
    setSelectedSerials((prev) => {
      const next = new Set(prev)
      for (const serial of filteredSerials) {
        if (next.size >= serialLimit) break
        next.add(serial.serialNumber)
      }
      return Array.from(next)
    })
  }

  const maxQty = stockItem.blocked > 0 ? stockItem.blocked : stockItem.available
  const hasSerials = (serials?.length ?? 0) > 0
  const serialLimit = hasSerials ? Math.min(Math.trunc(maxQty), serials?.length ?? 0) : 0
  const selectionLimitReached = hasSerials && selectedSerials.length >= serialLimit
  const qtyValue = hasSerials ? selectedSerials.length.toString() : qty
  const canSubmitSerials = hasSerials && selectedSerials.length > 0 && selectedSerials.length <= serialLimit
  const canSubmitQty = !hasSerials && !!qty && parseFloat(qty) > 0 && parseFloat(qty) <= maxQty
  const maxQtyDisplay = hasSerials ? serialLimit : maxQty
  const canSelectAllSerials = hasSerials && filteredSerials.length > 0 && selectedSerials.length < serialLimit

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-md rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">چیدن در قفسه</h2>
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
            {stockItem.productName || 'کالای نامشخص'}
            {stockItem.variantValue && <span className="text-emerald-600"> - {stockItem.variantValue}</span>}
          </div>
          <div className="mt-0.5 flex items-center gap-4 text-xs text-slate-600">
            <span className="font-mono">SKU: {stockItem.sku}</span>
            <span>•</span>
            {stockItem.blocked > 0 ? (
              <span className="inline-flex items-center gap-1 text-orange-600">
                <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
                </svg>
                موجودی مسدود: {stockItem.blocked.toLocaleString('fa-IR')}
              </span>
            ) : (
              <span>موجودی آزاد: {stockItem.available.toLocaleString('fa-IR')}</span>
            )}
          </div>
          {stockItem.lotNumber && (
            <div className="mt-1 text-xs text-slate-500">لات: {stockItem.lotNumber}</div>
          )}
        </div>

        <form onSubmit={handleSubmit} className="space-y-5">
          {/* Shelf Select */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              انتخاب قفسه <span className="text-red-500">*</span>
            </label>
            {loadingShelves ? (
              <div className="flex items-center justify-center py-4">
                <Spinner className="h-5 w-5 text-emerald-600" />
              </div>
            ) : !shelves || shelves.length === 0 ? (
              <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-3 text-sm text-yellow-800">
                هیچ قفسه فعالی برای این انبار پیدا نشد.
              </div>
            ) : (
              <select
                value={shelfId}
                onChange={(e) => setShelfId(e.target.value)}
                required
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              >
                <option value="">یک قفسه را انتخاب کنید...</option>
                {shelves.map((s) => (
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
              value={qtyValue}
              onChange={(e) => setQty(e.target.value)}
              min={hasSerials ? 1 : 0.01}
              max={maxQtyDisplay}
              step={hasSerials ? 1 : 0.01}
              required
              disabled={hasSerials}
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
            <p className="mt-1 text-xs text-slate-500">
              حداکثر: {maxQtyDisplay.toLocaleString('fa-IR')}{' '}
              {stockItem.blocked > 0 ? '(موجودی مسدود)' : '(موجودی آزاد)'}
            </p>
          </div>

          {hasSerials && (
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">Serials</label>
              <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
                <div className="flex items-center justify-between">
                  <span>Selected</span>
                  <span className="font-semibold">
                    {selectedSerials.length}/{serialLimit}
                  </span>
                </div>
              </div>

              <div className="mt-3 flex items-center gap-2">
                <input
                  value={serialSearch}
                  onChange={(e) => setSerialSearch(e.target.value)}
                  placeholder="Search serials..."
                  className="flex-1 rounded-lg border border-slate-300 px-3 py-2 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                />
                <button
                  type="button"
                  onClick={selectAllSerials}
                  disabled={!canSelectAllSerials}
                  className="rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 transition-colors hover:bg-slate-50 disabled:opacity-50"
                >
                  انتخاب همه
                </button>
                <button
                  type="button"
                  onClick={() => setSelectedSerials([])}
                  className="rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 transition-colors hover:bg-slate-50"
                >
                  Clear
                </button>
              </div>

              {loadingSerials ? (
                <div className="flex items-center justify-center py-4">
                  <Spinner className="h-5 w-5 text-emerald-600" />
                </div>
              ) : serialsError ? (
                <div className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-700">
                  {(serialsError as Error).message || 'Failed to load serials.'}
                </div>
              ) : filteredSerials.length === 0 ? (
                <div className="mt-3 rounded-lg border border-slate-200 bg-white p-3 text-sm text-slate-600">
                  No serials available.
                </div>
              ) : (
                <div className="mt-3 max-h-56 overflow-y-auto rounded-lg border border-slate-200 bg-white">
                  <div className="divide-y divide-slate-100">
                    {filteredSerials.map((serial) => {
                      const isChecked = selectedSerials.includes(serial.serialNumber)
                      const isDisabled = !isChecked && selectionLimitReached
                      return (
                        <label key={serial.serialNumber} className="flex items-start gap-3 px-4 py-2 text-sm">
                          <input
                            type="checkbox"
                            checked={isChecked}
                            disabled={isSubmitting || isDisabled}
                            onChange={() => toggleSerial(serial.serialNumber)}
                            className="mt-1 h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                          />
                          <div className="font-mono text-slate-900">{serial.serialNumber}</div>
                        </label>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Note */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">یادداشت (اختیاری)</label>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="یادداشت اضافه کنید..."
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
              disabled={isSubmitting || !shelfId || loadingShelves || (hasSerials ? !canSubmitSerials : !canSubmitQty)}
              className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Spinner className="h-4 w-4" />
                  در حال چیدن در قفسه...
                </>
              ) : (
                <>
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M13.5 16.875h3.375m0 0h3.375m-3.375 0V13.5m0 3.375v3.375M6 10.5h2.25a2.25 2.25 0 0 0 2.25-2.25V6a2.25 2.25 0 0 0-2.25-2.25H6A2.25 2.25 0 0 0 3.75 6v2.25A2.25 2.25 0 0 0 6 10.5Zm0 9.75h2.25A2.25 2.25 0 0 0 10.5 18v-2.25a2.25 2.25 0 0 0-2.25-2.25H6a2.25 2.25 0 0 0-2.25 2.25V18A2.25 2.25 0 0 0 6 19.5Zm9.75-9.75H18a2.25 2.25 0 0 0 2.25-2.25V6A2.25 2.25 0 0 0 18 3.75h-2.25A2.25 2.25 0 0 0 13.5 6v2.25a2.25 2.25 0 0 0 2.25 2.25Z" />
                  </svg>
                  چیدن در قفسه
                </>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
