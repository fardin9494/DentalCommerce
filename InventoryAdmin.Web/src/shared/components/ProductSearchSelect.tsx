import { useState, useEffect, useRef, useCallback } from 'react'
import { useProductSearch, useProductDetail } from '../hooks/useProducts'
import { Spinner } from './Spinner'
import type { ProductListItem, Variant } from '../api/catalog'

export interface ProductSelection {
  productId: string
  productName: string
  productCode: string
  variantId?: string
  variantValue?: string
  sku: string
}

interface ProductSearchSelectProps {
  onSelect: (selection: ProductSelection) => void
  onCancel?: () => void
  className?: string
}

export function ProductSearchSelect({ onSelect, onCancel, className = '' }: ProductSearchSelectProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedProduct, setSelectedProduct] = useState<ProductListItem | null>(null)
  const [selectedVariant, setSelectedVariant] = useState<Variant | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search)
    }, 300)
    return () => clearTimeout(timer)
  }, [search])

  // Search products
  const { data: searchResult, isLoading: searching } = useProductSearch(
    debouncedSearch,
    1,
    15,
    !selectedProduct
  )

  // Get product detail when selected (for variants)
  const { data: productDetail, isLoading: loadingDetail } = useProductDetail(
    selectedProduct?.id,
    !!selectedProduct
  )

  // Close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  const handleSelectProduct = useCallback((product: ProductListItem) => {
    setSelectedProduct(product)
    setSelectedVariant(null)
    setSearch('')
    setIsOpen(false)
  }, [])

  const handleSelectVariant = useCallback((variant: Variant) => {
    setSelectedVariant(variant)
  }, [])

  const handleConfirm = useCallback(() => {
    if (!selectedProduct) return

    const hasVariants = productDetail?.variants && productDetail.variants.length > 0
    const activeVariants = productDetail?.variants?.filter(v => v.isActive) || []

    // If product has active variants but none selected, show error
    if (hasVariants && activeVariants.length > 0 && !selectedVariant) {
      return // Don't allow confirm without variant selection
    }

    const selection: ProductSelection = {
      productId: selectedProduct.id,
      productName: selectedProduct.name,
      productCode: selectedProduct.code,
      sku: selectedVariant?.sku || selectedProduct.code,
    }

    if (selectedVariant) {
      selection.variantId = selectedVariant.id
      selection.variantValue = selectedVariant.value
    }

    onSelect(selection)
  }, [selectedProduct, selectedVariant, productDetail, onSelect])

  const handleClear = useCallback(() => {
    setSelectedProduct(null)
    setSelectedVariant(null)
    setSearch('')
    inputRef.current?.focus()
  }, [])

  const activeVariants = productDetail?.variants?.filter(v => v.isActive) || []
  const hasActiveVariants = activeVariants.length > 0
  const canConfirm = selectedProduct && (!hasActiveVariants || selectedVariant)

  return (
    <div className={`space-y-4 ${className}`} ref={dropdownRef}>
      {/* Search Input or Selected Product */}
      {!selectedProduct ? (
        <div className="relative">
          <label className="mb-2 block text-sm font-medium text-slate-700">
            جستجوی محصول <span className="text-red-500">*</span>
          </label>
          <div className="relative">
            <input
              ref={inputRef}
              type="text"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value)
                setIsOpen(true)
              }}
              onFocus={() => setIsOpen(true)}
              placeholder="نام یا کد محصول را وارد کنید..."
              className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              autoComplete="off"
            />
            <div className="absolute left-3 top-1/2 -translate-y-1/2">
              {searching ? (
                <Spinner className="h-4 w-4 text-emerald-600" />
              ) : (
                <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
                </svg>
              )}
            </div>
          </div>

          {/* Search Results Dropdown */}
          {isOpen && debouncedSearch.length >= 2 && (
            <div className="absolute z-50 mt-1 w-full rounded-lg border border-slate-200 bg-white shadow-lg">
              {searching ? (
                <div className="flex items-center justify-center py-8">
                  <Spinner className="h-6 w-6 text-emerald-600" />
                </div>
              ) : !searchResult || searchResult.items.length === 0 ? (
                <div className="py-6 text-center text-sm text-slate-500">
                  محصولی یافت نشد
                </div>
              ) : (
                <div className="max-h-64 overflow-y-auto">
                  {searchResult.items.map((product) => (
                    <button
                      key={product.id}
                      type="button"
                      onClick={() => handleSelectProduct(product)}
                      className="flex w-full items-center gap-3 px-4 py-3 text-right transition-colors hover:bg-slate-50"
                    >
                      {product.mainImageUrl ? (
                        <img
                          src={product.mainImageUrl}
                          alt={product.name}
                          className="h-10 w-10 rounded-lg object-cover"
                        />
                      ) : (
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-slate-100">
                          <svg className="h-5 w-5 text-slate-400" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="m2.25 15.75 5.159-5.159a2.25 2.25 0 0 1 3.182 0l5.159 5.159m-1.5-1.5 1.409-1.409a2.25 2.25 0 0 1 3.182 0l2.909 2.909m-18 3.75h16.5a1.5 1.5 0 0 0 1.5-1.5V6a1.5 1.5 0 0 0-1.5-1.5H3.75A1.5 1.5 0 0 0 2.25 6v12a1.5 1.5 0 0 0 1.5 1.5Zm10.5-11.25h.008v.008h-.008V8.25Zm.375 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z" />
                          </svg>
                        </div>
                      )}
                      <div className="flex-1 min-w-0">
                        <div className="font-medium text-slate-900 truncate">{product.name}</div>
                        <div className="flex items-center gap-2 text-xs text-slate-500">
                          <span className="font-mono">{product.code}</span>
                          {product.brandName && (
                            <>
                              <span>•</span>
                              <span>{product.brandName}</span>
                            </>
                          )}
                        </div>
                      </div>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          {search.length > 0 && search.length < 2 && (
            <p className="mt-1 text-xs text-slate-500">حداقل ۲ کاراکتر وارد کنید</p>
          )}
        </div>
      ) : (
        /* Selected Product Display */
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 p-4">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-lg bg-emerald-100">
                <svg className="h-6 w-6 text-emerald-600" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                </svg>
              </div>
              <div>
                <div className="font-medium text-slate-900">{selectedProduct.name}</div>
                <div className="flex items-center gap-2 text-sm text-slate-600">
                  <span className="font-mono">{selectedProduct.code}</span>
                  {selectedProduct.brandName && (
                    <>
                      <span>•</span>
                      <span>{selectedProduct.brandName}</span>
                    </>
                  )}
                </div>
              </div>
            </div>
            <button
              type="button"
              onClick={handleClear}
              className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-white hover:text-slate-600"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
        </div>
      )}

      {/* Variant Selection */}
      {selectedProduct && (
        <div>
          {loadingDetail ? (
            <div className="flex items-center justify-center py-4">
              <Spinner className="h-5 w-5 text-emerald-600" />
              <span className="mr-2 text-sm text-slate-600">بارگذاری واریانت‌ها...</span>
            </div>
          ) : hasActiveVariants ? (
            <div>
              <label className="mb-2 block text-sm font-medium text-slate-700">
                انتخاب واریانت <span className="text-red-500">*</span>
              </label>
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                {activeVariants.map((variant) => (
                  <button
                    key={variant.id}
                    type="button"
                    onClick={() => handleSelectVariant(variant)}
                    className={`rounded-lg border-2 px-3 py-2.5 text-sm font-medium transition-all ${
                      selectedVariant?.id === variant.id
                        ? 'border-emerald-500 bg-emerald-50 text-emerald-700'
                        : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                    }`}
                  >
                    <div>{variant.value}</div>
                    <div className="mt-0.5 font-mono text-xs text-slate-400">{variant.sku}</div>
                  </button>
                ))}
              </div>
            </div>
          ) : productDetail ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
              این محصول واریانت ندارد
            </div>
          ) : null}
        </div>
      )}

      {/* Actions */}
      <div className="flex items-center justify-end gap-3 pt-2">
        {onCancel && (
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100"
          >
            انصراف
          </button>
        )}
        <button
          type="button"
          onClick={handleConfirm}
          disabled={!canConfirm}
          className="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" d="m4.5 12.75 6 6 9-13.5" />
          </svg>
          تایید انتخاب
        </button>
      </div>
    </div>
  )
}

