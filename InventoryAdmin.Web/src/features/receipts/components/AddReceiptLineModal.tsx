import { useState } from 'react'
import { ProductSearchSelect, type ProductSelection } from '@/shared/components/ProductSearchSelect'
import { Spinner } from '@/shared/components/Spinner'
import DatePicker from 'react-multi-date-picker'
import DateObject from 'react-date-object'
import persian from 'react-date-object/calendars/persian'
import persian_fa from 'react-date-object/locales/persian_fa'

interface AddReceiptLineModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: {
    productId: string
    variantId?: string
    qty: number
    lotNumber?: string
    expiryDateUtc?: string
    unitCost?: number
  }) => void
  isSubmitting?: boolean
}

export function AddReceiptLineModal({ isOpen, onClose, onSubmit, isSubmitting }: AddReceiptLineModalProps) {
  const [step, setStep] = useState<'product' | 'details'>('product')
  const [selectedProduct, setSelectedProduct] = useState<ProductSelection | null>(null)
  const [qty, setQty] = useState('1')
  const [lotNumber, setLotNumber] = useState('')
  const [expiryDate, setExpiryDate] = useState<DateObject | null>(null)
  const [unitCost, setUnitCost] = useState('')

  if (!isOpen) return null

  function handleProductSelect(selection: ProductSelection) {
    setSelectedProduct(selection)
    setStep('details')
  }

  function handleBack() {
    setStep('product')
    setSelectedProduct(null)
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!selectedProduct) return

    const qtyNum = parseFloat(qty)
    if (isNaN(qtyNum) || qtyNum <= 0) return

    const expiryDateUtc = expiryDate ? expiryDate.toDate().toISOString() : undefined

    onSubmit({
      productId: selectedProduct.productId,
      variantId: selectedProduct.variantId,
      qty: qtyNum,
      lotNumber: lotNumber.trim() || undefined,
      // در صورت عدم انتخاب تاریخ، فیلد ارسال نمی‌شود
      ...(expiryDateUtc !== undefined ? { expiryDateUtc } : {}),
      unitCost: unitCost ? parseFloat(unitCost) : undefined,
    })
  }

  function handleClose() {
    setStep('product')
    setSelectedProduct(null)
    setQty('1')
    setLotNumber('')
    setExpiryDate(null)
    setUnitCost('')
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-2xl min-h-[600px] rounded-2xl bg-white p-6 shadow-2xl max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <div className="flex items-center gap-3">
            {step === 'details' && (
              <button
                onClick={handleBack}
                className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="M9 15 3 9m0 0 6-6M3 9h12a6 6 0 0 1 0 12h-3" />
                </svg>
              </button>
            )}
            <h2 className="text-xl font-bold text-slate-900">
              {step === 'product' ? 'انتخاب محصول' : 'جزئیات خط رسید'}
            </h2>
          </div>
          <button
            onClick={handleClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Step 1: Product Selection */}
        {step === 'product' && (
          <ProductSearchSelect
            onSelect={handleProductSelect}
            onCancel={handleClose}
          />
        )}

        {/* Step 2: Details Form */}
        {step === 'details' && selectedProduct && (
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Selected Product Summary */}
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
              <div className="text-sm font-medium text-emerald-800">محصول انتخاب شده</div>
              <div className="mt-1 font-medium text-slate-900">
                {selectedProduct.productName}
                {selectedProduct.variantValue && (
                  <span className="text-emerald-600"> - {selectedProduct.variantValue}</span>
                )}
              </div>
              <div className="mt-0.5 font-mono text-xs text-slate-500">SKU: {selectedProduct.sku}</div>
            </div>

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
              <label className="mb-2 block text-sm font-medium text-slate-700">شماره لات (اختیاری)</label>
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
              <label className="mb-2 block text-sm font-medium text-slate-700">تاریخ انقضا (شمسی / اختیاری)</label>
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
              <label className="mb-2 block text-sm font-medium text-slate-700">هزینه واحد (اختیاری)</label>
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
            <div className="flex items-center justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={handleBack}
                disabled={isSubmitting}
                className="rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:opacity-50"
              >
                بازگشت
              </button>
              <button
                type="submit"
                disabled={isSubmitting || !qty || parseFloat(qty) <= 0}
                className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
              >
                {isSubmitting ? (
                  <>
                    <Spinner className="h-4 w-4" />
                    در حال افزودن...
                  </>
                ) : (
                  <>
                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                    </svg>
                    افزودن به رسید
                  </>
                )}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  )
}

