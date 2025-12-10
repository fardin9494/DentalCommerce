import { useState, useEffect, useMemo } from 'react'
import { Spinner } from '@/shared/components/Spinner'
import { useProductNames } from '@/shared/hooks/useProductNames'
import DatePicker from 'react-multi-date-picker'
import DateObject from 'react-date-object'
import persian from 'react-date-object/calendars/persian'
import persian_fa from 'react-date-object/locales/persian_fa'
import type { ReceiptLine } from '../types'

interface EditReceiptLineModalProps {
  isOpen: boolean
  line: ReceiptLine | null
  onClose: () => void
  onSubmit: (data: {
    qty?: number
    lotNumber?: string | null
    expiryDateUtc?: string | null
    unitCost?: number | null
  }) => void
  isSubmitting?: boolean
}

export function EditReceiptLineModal({ isOpen, line, onClose, onSubmit, isSubmitting }: EditReceiptLineModalProps) {
  const [qty, setQty] = useState('')
  const [lotNumber, setLotNumber] = useState('')
  const [expiryDate, setExpiryDate] = useState<DateObject | null>(null)
  const [unitCost, setUnitCost] = useState('')
  
  const productIds = useMemo(() => (line ? [line.productId] : []), [line?.productId])
  const { getProductName, getVariantName } = useProductNames(productIds)

  // Initialize form when line changes
  useEffect(() => {
    if (line) {
      setQty(line.qty.toString())
      setLotNumber(line.lotNumber || '')
      if (line.expiryDateUtc) {
        const g = new Date(line.expiryDateUtc)
        setExpiryDate(new DateObject({ date: g, calendar: persian, locale: persian_fa }))
      } else {
        setExpiryDate(null)
      }
      setUnitCost(line.unitCost?.toString() || '')
    }
  }, [line])

  if (!isOpen || !line) return null

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()

    const qtyNum = parseFloat(qty)
    if (isNaN(qtyNum) || qtyNum <= 0) return

    const expiryDateUtc = expiryDate ? expiryDate.toDate().toISOString() : null

    onSubmit({
      qty: qtyNum,
      lotNumber: lotNumber.trim() || null,
      expiryDateUtc,
      unitCost: unitCost ? parseFloat(unitCost) : null,
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
          <h2 className="text-xl font-bold text-slate-900">ویرایش خط رسید</h2>
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
          <div className="mt-1 text-sm text-slate-700">
            <div className="font-medium text-slate-900">محصول: {getProductName(line.productId)}</div>
            {line.variantId && (
              <div className="mt-0.5 text-sm text-emerald-600">
                واریانت: {getVariantName(line.variantId) || line.variantId.substring(0, 8) + '...'}
              </div>
            )}
          </div>
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
              min="0.01"
              step="0.01"
              required
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
          </div>

          {/* Lot Number */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">شماره لات</label>
            <input
              type="text"
              value={lotNumber}
              onChange={(e) => setLotNumber(e.target.value)}
              placeholder="مثال: LOT-2024-001"
              className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
            />
          </div>

          {/* Expiry Date */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">تاریخ انقضا (شمسی)</label>
            <DatePicker
              value={expiryDate}
              onChange={(date) => {
                if (Array.isArray(date)) {
                  setExpiryDate((date[0] as DateObject | null) ?? null)
                } else {
                  setExpiryDate(date as DateObject | null)
                }
              }}
              calendar={persian}
              locale={persian_fa}
              calendarPosition="bottom-center"
              portal
              editable={false}
              inputClass="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              placeholder="انتخاب تاریخ"
              format="YYYY/MM/DD"
            />
            <p className="mt-1 text-xs text-slate-500">انتخاب از تقویم شمسی؛ در سیستم به میلادی ذخیره می‌شود.</p>
          </div>

          {/* Unit Cost */}
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">هزینه واحد</label>
            <div className="relative">
              <input
                type="number"
                value={unitCost}
                onChange={(e) => setUnitCost(e.target.value)}
                min="0"
                step="1"
                placeholder="0"
                className="w-full rounded-lg border border-slate-300 py-2.5 pl-16 pr-4 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              />
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-slate-400">ریال</span>
            </div>
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
              disabled={isSubmitting || !qty || parseFloat(qty) <= 0}
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

