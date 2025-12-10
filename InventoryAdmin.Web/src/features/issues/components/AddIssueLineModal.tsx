import { useState } from 'react'
import { ProductSearchSelect, type ProductSelection } from '@/shared/components/ProductSearchSelect'
import { Spinner } from '@/shared/components/Spinner'

interface AddIssueLineModalProps {
  isOpen: boolean
  onClose: () => void
  onSubmit: (data: { productId: string; variantId: string | null; qty: number }) => Promise<void>
  isSubmitting?: boolean
}

export function AddIssueLineModal({ isOpen, onClose, onSubmit, isSubmitting = false }: AddIssueLineModalProps) {
  if (!isOpen) return null
  const [selectedProduct, setSelectedProduct] = useState<ProductSelection | null>(null)
  const [qty, setQty] = useState('1')
  const [qtyError, setQtyError] = useState('')

  function handleProductSelect(selection: ProductSelection) {
    setSelectedProduct(selection)
  }

  function handleQtyChange(e: React.ChangeEvent<HTMLInputElement>) {
    const value = e.target.value
    setQty(value)
    
    if (value.trim() === '') {
      setQtyError('مقدار الزامی است')
      return
    }
    
    const numValue = parseFloat(value)
    if (isNaN(numValue) || numValue <= 0) {
      setQtyError('مقدار باید عدد مثبت باشد')
      return
    }
    
    setQtyError('')
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    
    if (!selectedProduct) {
      return
    }
    
    if (qtyError || !qty.trim()) {
      setQtyError('لطفاً مقدار معتبر وارد کنید')
      return
    }
    
    const numQty = parseFloat(qty)
    if (isNaN(numQty) || numQty <= 0) {
      setQtyError('مقدار باید عدد مثبت باشد')
      return
    }

    try {
      await onSubmit({
        productId: selectedProduct.productId,
        variantId: selectedProduct.variantId || null,
        qty: numQty,
      })
      
      // Reset form
      setSelectedProduct(null)
      setQty('1')
      setQtyError('')
      onClose()
    } catch (error) {
      // Error handling is done in the parent component
    }
  }

  function handleClose() {
    if (isSubmitting) return
    setSelectedProduct(null)
    setQty('1')
    setQtyError('')
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={handleClose} />

      {/* Modal */}
      <div className="relative w-full max-w-2xl rounded-2xl bg-white p-6 shadow-2xl">
        {/* Header */}
        <div className="mb-6 flex items-center justify-between">
          <h2 className="text-xl font-bold text-slate-900">افزودن خط جدید</h2>
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

        <form onSubmit={handleSubmit} className="space-y-6">
        {/* Product Selection */}
        <ProductSearchSelect
          onSelect={handleProductSelect}
          onCancel={handleClose}
        />

        {/* Quantity Input */}
        {selectedProduct && (
          <div>
            <label className="mb-2 block text-sm font-medium text-slate-700">
              مقدار <span className="text-red-500">*</span>
            </label>
            <input
              type="number"
              value={qty}
              onChange={handleQtyChange}
              min="0.01"
              step="0.01"
              required
              disabled={isSubmitting}
              className={`w-full rounded-lg border px-3 py-2.5 text-sm transition-colors focus:outline-none focus:ring-2 ${
                qtyError
                  ? 'border-red-300 focus:border-red-500 focus:ring-red-500/20'
                  : 'border-slate-300 focus:border-emerald-500 focus:ring-emerald-500/20'
              } disabled:bg-slate-100 disabled:cursor-not-allowed`}
              placeholder="1"
            />
            {qtyError && (
              <p className="mt-1 text-xs text-red-600">{qtyError}</p>
            )}
          </div>
        )}

        {/* Selected Product Summary */}
        {selectedProduct && (
          <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
            <div className="text-sm">
              <div className="font-medium text-slate-900">محصول انتخاب شده:</div>
              <div className="mt-1 text-slate-600">
                <span className="font-medium">{selectedProduct.productName}</span>
                {selectedProduct.variantValue && (
                  <span className="mr-2"> - {selectedProduct.variantValue}</span>
                )}
              </div>
              <div className="mt-1 text-xs text-slate-500">
                <span className="font-mono">SKU: {selectedProduct.sku}</span>
                {selectedProduct.productCode && (
                  <span className="mr-2"> | کد: {selectedProduct.productCode}</span>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex items-center justify-end gap-3 border-t border-slate-200 pt-4">
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
            disabled={!selectedProduct || !!qtyError || isSubmitting}
            className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? (
              <>
                <Spinner className="h-4 w-4 text-white" />
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
      </div>
    </div>
  )
}

