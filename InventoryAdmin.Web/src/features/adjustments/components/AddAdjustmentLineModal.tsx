import { useMemo, useState } from 'react'
import { StockProductSelect, type StockProductSelection } from '@/shared/components/StockProductSelect'
import { Spinner } from '@/shared/components/Spinner'
import DatePicker from 'react-multi-date-picker'
import DateObject from 'react-date-object'
import persian from 'react-date-object/calendars/persian'
import persian_fa from 'react-date-object/locales/persian_fa'
import { useStockItems } from '@/features/stock-items/queries'
import type { StockItem } from '@/features/stock-items/api'

interface AddAdjustmentLineModalProps {
  isOpen: boolean
  warehouseId: string
  onClose: () => void
  onSubmit: (data: {
    productId: string
    variantId?: string
    lotNumber?: string
    expiryDateUtc?: string
    qtyDelta: number
  }) => void
  isSubmitting?: boolean
}

export function AddAdjustmentLineModal({
  isOpen,
  warehouseId,
  onClose,
  onSubmit,
  isSubmitting,
}: AddAdjustmentLineModalProps) {
  const [step, setStep] = useState<'product' | 'details'>('product')
  const [selectedProduct, setSelectedProduct] = useState<StockProductSelection | null>(null)
  const [qtyDelta, setQtyDelta] = useState('1')
  const [direction, setDirection] = useState<'increase' | 'decrease'>('increase')
  const [lotNumber, setLotNumber] = useState('')
  const [expiryDate, setExpiryDate] = useState<DateObject | null>(null)
  const [selectedStockItem, setSelectedStockItem] = useState<StockItem | null>(null)

  const stockFilters = useMemo(() => {
    if (!selectedProduct) return null
    return {
      warehouseId,
      productId: selectedProduct.productId,
      variantId: selectedProduct.variantId || undefined,
      pageSize: 50,
    }
  }, [selectedProduct, warehouseId])

  const { data: stockItemsData, isLoading: loadingStock } = useStockItems(stockFilters || {}, { enabled: !!stockFilters })

  if (!isOpen) return null

  function handleProductSelect(selection: StockProductSelection) {
    setSelectedProduct(selection)
    setStep('details')
    setSelectedStockItem(null)
    setLotNumber('')
    setExpiryDate(null)
  }

  function handleBack() {
    setStep('product')
    setSelectedProduct(null)
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!selectedProduct) return

    const qtyDeltaNum = parseFloat(qtyDelta)
    if (isNaN(qtyDeltaNum) || qtyDeltaNum <= 0) return

    // برای کاهش یا افزایش موجودی، باید یک موجودی انتخاب شود تا لات/انقضا مشخص باشد
    if (!selectedStockItem) return

    const signedQty = direction === 'increase' ? qtyDeltaNum : -qtyDeltaNum
    const expiryDateUtc = expiryDate ? expiryDate.toDate().toISOString() : undefined

    onSubmit({
      productId: selectedProduct.productId,
      variantId: selectedProduct.variantId,
      lotNumber: lotNumber.trim() || selectedStockItem.lotNumber || undefined,
      expiryDateUtc: expiryDateUtc ?? selectedStockItem.expiryDate ?? undefined,
      qtyDelta: signedQty,
    })
  }

  function handleClose() {
    setStep('product')
    setSelectedProduct(null)
    setQtyDelta('1')
    setDirection('increase')
    setLotNumber('')
    setExpiryDate(null)
    setSelectedStockItem(null)
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
              {step === 'product' ? 'انتخاب محصول' : 'جزئیات خط اصلاح'}
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
          <div className="space-y-4">
            <StockProductSelect warehouseId={warehouseId} onSelect={handleProductSelect} />
          </div>
        )}

        {/* Step 2: Details */}
        {step === 'details' && selectedProduct && (
          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Selected Product Info */}
            <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
              <div className="text-sm font-medium text-emerald-800">محصول انتخابی</div>
              <div className="mt-1 font-medium text-slate-900">
                {selectedProduct.productName || 'نامشخص'}
                {selectedProduct.variantValue && (
                  <span className="text-emerald-600"> - {selectedProduct.variantValue}</span>
                )}
              </div>
              <div className="mt-0.5 text-xs text-slate-600">
                <span className="font-mono">SKU: {selectedProduct.sku}</span>
              </div>
            </div>

            {/* Select stock item (lot) */}
            <div className="space-y-3">
              <div className="text-sm font-medium text-slate-700">انتخاب موجودی (لات)</div>
              <div className="rounded-lg border border-slate-200">
                <div className="p-3 border-b border-slate-100 bg-slate-50 text-xs text-slate-500">
                  موجودی‌های همین انبار برای محصول انتخاب‌شده
                </div>
                <div className="max-h-64 overflow-y-auto divide-y divide-slate-100">
                  {loadingStock ? (
                    <div className="flex items-center justify-center py-6">
                      <Spinner className="h-5 w-5 text-emerald-600" />
                    </div>
                  ) : (stockItemsData?.items?.length || 0) === 0 ? (
                    <div className="py-6 text-center text-sm text-slate-500">موجودی‌ای یافت نشد</div>
                  ) : (
                    stockItemsData!.items.map((item) => (
                      <button
                        key={item.id}
                        type="button"
                        onClick={() => {
                          setSelectedStockItem(item)
                          setLotNumber(item.lotNumber || '')
                          setExpiryDate(
                            item.expiryDate
                              ? new DateObject({ date: new Date(item.expiryDate), calendar: persian, locale: persian_fa })
                              : null
                          )
                        }}
                        className={`w-full text-right px-4 py-3 transition-colors ${
                          selectedStockItem?.id === item.id ? 'bg-emerald-50 border-l-4 border-emerald-500' : 'hover:bg-slate-50'
                        }`}
                      >
                        <div className="flex items-center justify-between">
                          <div>
                            <div className="font-medium text-slate-900">
                              لات: {item.lotNumber || 'نامشخص'}
                            </div>
                            <div className="text-xs text-slate-500 mt-0.5">
                              انقضا: {item.expiryDate ? new Date(item.expiryDate).toLocaleDateString('fa-IR') : 'نامشخص'}
                            </div>
                          </div>
                          <div className="text-right text-xs text-slate-600">
                            موجودی: {item.onHand.toLocaleString('fa-IR')}
                            <div className="text-emerald-600">آزاد: {item.available.toLocaleString('fa-IR')}</div>
                          </div>
                        </div>
                      </button>
                    ))
                  )}
                </div>
              </div>
            </div>

            {/* Qty Delta */}
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                نوع اصلاح و مقدار <span className="text-red-500">*</span>
              </label>
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div className="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <input
                    id="adj-inc"
                    type="radio"
                    name="adjustment-direction"
                    value="increase"
                    checked={direction === 'increase'}
                    onChange={() => setDirection('increase')}
                    className="h-4 w-4 text-emerald-600 focus:ring-emerald-500"
                  />
                  <label htmlFor="adj-inc" className="text-sm font-medium text-slate-800">
                    افزایش موجودی
                  </label>
                </div>
                <div className="flex items-center gap-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <input
                    id="adj-dec"
                    type="radio"
                    name="adjustment-direction"
                    value="decrease"
                    checked={direction === 'decrease'}
                    onChange={() => setDirection('decrease')}
                    className="h-4 w-4 text-emerald-600 focus:ring-emerald-500"
                  />
                  <label htmlFor="adj-dec" className="text-sm font-medium text-slate-800">
                    کاهش موجودی
                  </label>
                </div>
              </div>
              <div className="mt-3">
                <input
                  type="number"
                  value={qtyDelta}
                  onChange={(e) => setQtyDelta(e.target.value)}
                  step="0.01"
                  min="0.01"
                  required
                  placeholder="مثال: 10"
                  className="w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
                />
                <p className="mt-1 text-xs text-slate-500">
                  ابتدا نوع اصلاح را انتخاب کنید، سپس مقدار مثبت وارد کنید.
                </p>
              </div>
            </div>

            {/* Lot Number */}
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">شماره لات</label>
              <input
                type="text"
                value={lotNumber}
                readOnly
                className="w-full rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm text-slate-600"
              />
              <p className="mt-1 text-xs text-slate-500">از موجودی انتخاب‌شده خوانده می‌شود.</p>
            </div>

            {/* Expiry Date */}
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">تاریخ انقضا</label>
              <input
                type="text"
                readOnly
                value={expiryDate ? expiryDate.format('YYYY/MM/DD') : 'نامشخص'}
                className="w-full rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm text-slate-600"
              />
              <p className="mt-1 text-xs text-slate-500">از موجودی انتخاب‌شده خوانده می‌شود.</p>
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
                disabled={isSubmitting || parseFloat(qtyDelta) === 0}
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
                    افزودن خط
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

