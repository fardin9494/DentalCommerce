import { useState, useRef, useEffect } from 'react'
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

  // Refs for auto-focus
  const qtyRef = useRef<HTMLInputElement>(null)
  const lotRef = useRef<HTMLInputElement>(null)
  const formRef = useRef<HTMLFormElement>(null)

  // Auto-focus on quantity field when entering details step
  useEffect(() => {
    if (step === 'details' && qtyRef.current) {
      // Small delay to ensure DOM is ready
      const timer = setTimeout(() => {
        qtyRef.current?.focus()
        qtyRef.current?.select()
      }, 100)
      return () => clearTimeout(timer)
    }
  }, [step])

  if (!isOpen) return null

  function handleProductSelect(selection: ProductSelection) {
    setSelectedProduct(selection)
    setStep('details')
  }

  function handleBack() {
    setStep('product')
    setSelectedProduct(null)
  }

  function handleSubmit(e: React.FormEvent, continueAdding = false) {
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

    if (continueAdding) {
      // Reset for next entry but keep modal open
      resetFormForContinue()
    }
  }

  function resetFormForContinue() {
    setStep('product')
    setSelectedProduct(null)
    setQty('1')
    setLotNumber('')
    setExpiryDate(null)
    setUnitCost('')
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

  // Handle keyboard shortcuts
  function handleFormKeyDown(e: React.KeyboardEvent) {
    // Ctrl+Enter or Cmd+Enter to submit
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault()
      if (selectedProduct && qty && parseFloat(qty) > 0 && !isSubmitting) {
        handleSubmit(e as unknown as React.FormEvent)
      }
    }

    // Ctrl+Shift+Enter to submit and continue
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && e.shiftKey) {
      e.preventDefault()
      if (selectedProduct && qty && parseFloat(qty) > 0 && !isSubmitting) {
        handleSubmit(e as unknown as React.FormEvent, true)
      }
    }
  }

  // Handle Enter key on individual fields to move to next field
  function handleFieldKeyDown(e: React.KeyboardEvent<HTMLInputElement>, nextRef?: React.RefObject<HTMLInputElement>) {
    if (e.key === 'Enter' && !e.ctrlKey && !e.metaKey && !e.shiftKey) {
      e.preventDefault()
      if (nextRef?.current) {
        nextRef.current.focus()
        nextRef.current.select?.()
      }
    }
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
                title="بازگشت به انتخاب محصول"
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
            title="بستن (Esc)"
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
            autoConfirm={true}
          />
        )}

        {/* Step 2: Details Form */}
        {step === 'details' && selectedProduct && (
          <form
            ref={formRef}
            onSubmit={(e) => handleSubmit(e)}
            onKeyDown={handleFormKeyDown}
            className="space-y-5"
          >
            {/* Selected Product Summary */}
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
              <div className="flex items-start justify-between">
                <div>
                  <div className="text-sm font-medium text-emerald-800">محصول انتخاب شده</div>
                  <div className="mt-1 font-medium text-slate-900">
                    {selectedProduct.productName}
                    {selectedProduct.variantValue && (
                      <span className="text-emerald-600"> - {selectedProduct.variantValue}</span>
                    )}
                  </div>
                  <div className="mt-0.5 font-mono text-xs text-slate-500">SKU: {selectedProduct.sku}</div>
                </div>
                <button
                  type="button"
                  onClick={handleBack}
                  className="rounded-lg p-1.5 text-emerald-600 transition-colors hover:bg-emerald-100"
                  title="تغییر محصول"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Z" />
                  </svg>
                </button>
              </div>
            </div>

            {/* Quantity */}
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                تعداد <span className="text-red-500">*</span>
              </label>
              <input
                ref={qtyRef}
                type="number"
                value={qty}
                onChange={(e) => setQty(e.target.value)}
                onKeyDown={(e) => handleFieldKeyDown(e, lotRef)}
                min="0.01"
                step="0.01"
                required
                className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                placeholder="تعداد را وارد کنید"
              />
              <p className="mt-1 text-xs text-slate-400">Enter برای رفتن به فیلد بعدی</p>
            </div>

            {/* Lot Number */}
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">شماره لات (اختیاری)</label>
              <input
                ref={lotRef}
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

            {/* Keyboard Shortcuts Hint */}
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <div className="text-xs font-medium text-slate-600 mb-2">میانبرهای کیبورد:</div>
              <div className="flex flex-wrap gap-3 text-xs text-slate-500">
                <div className="flex items-center gap-1">
                  <kbd className="rounded bg-white px-1.5 py-0.5 font-mono text-[10px] shadow-sm border">Ctrl</kbd>
                  <span>+</span>
                  <kbd className="rounded bg-white px-1.5 py-0.5 font-mono text-[10px] shadow-sm border">Enter</kbd>
                  <span className="mr-1">افزودن</span>
                </div>
                <div className="flex items-center gap-1">
                  <kbd className="rounded bg-white px-1.5 py-0.5 font-mono text-[10px] shadow-sm border">Ctrl</kbd>
                  <span>+</span>
                  <kbd className="rounded bg-white px-1.5 py-0.5 font-mono text-[10px] shadow-sm border">Shift</kbd>
                  <span>+</span>
                  <kbd className="rounded bg-white px-1.5 py-0.5 font-mono text-[10px] shadow-sm border">Enter</kbd>
                  <span className="mr-1">افزودن و ادامه</span>
                </div>
              </div>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between gap-3 pt-2">
              <button
                type="button"
                onClick={handleBack}
                disabled={isSubmitting}
                className="rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:opacity-50"
              >
                بازگشت
              </button>

              <div className="flex items-center gap-2">
                {/* Add and Continue Button */}
                <button
                  type="button"
                  onClick={(e) => handleSubmit(e, true)}
                  disabled={isSubmitting || !qty || parseFloat(qty) <= 0}
                  className="inline-flex items-center gap-2 rounded-lg border-2 border-emerald-600 bg-white px-4 py-2 text-sm font-medium text-emerald-600 transition-colors hover:bg-emerald-50 disabled:opacity-50"
                  title="Ctrl+Shift+Enter"
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
                      افزودن و ادامه
                    </>
                  )}
                </button>

                {/* Submit Button */}
                <button
                  type="submit"
                  disabled={isSubmitting || !qty || parseFloat(qty) <= 0}
                  className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50"
                  title="Ctrl+Enter"
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
            </div>
          </form>
        )}
      </div>
    </div>
  )
}


