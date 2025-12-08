import { useState } from 'react'
import { ProductSearchSelect, type ProductSelection } from '@/shared/components/ProductSearchSelect'
import { Spinner } from '@/shared/components/Spinner'

interface AddTransferLineModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { productId: string; variantId?: string; qty: number }) => void
  isSubmitting?: boolean
}

export function AddTransferLineModal({ isOpen, onClose, onSubmit, isSubmitting }: AddTransferLineModalProps) {
  const [step, setStep] = useState<'product' | 'details'>('product')
  const [selectedProduct, setSelectedProduct] = useState<ProductSelection | null>(null)
  const [qty, setQty] = useState('1')

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

    onSubmit({
      productId: selectedProduct.productId,
      variantId: selectedProduct.variantId,
      qty: qtyNum,
    })
  }

  function handleClose() {
    setStep('product')
    setSelectedProduct(null)
    setQty('1')
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-2xl min-h-[500px] rounded-2xl bg-white p-6 shadow-2xl max-h-[90vh] overflow-y-auto">
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
              {step === 'product' ? 'انتخاب محصول' : 'مشخصات خط انتقال'}
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
            <ProductSearchSelect onSelect={handleProductSelect} />
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
                <span className="font-mono">SKU: {selectedProduct.productCode}</span>
              </div>
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
                disabled={isSubmitting || parseFloat(qty) <= 0}
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

