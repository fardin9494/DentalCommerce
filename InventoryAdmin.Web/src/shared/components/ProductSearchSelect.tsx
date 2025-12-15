import { useState, useEffect, useRef, useCallback, useLayoutEffect } from 'react'
import { createPortal } from 'react-dom'
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
  /** اگر true باشد، بعد از انتخاب واریانت خودکار تایید می‌شود */
  autoConfirm?: boolean
}

export function ProductSearchSelect({ onSelect, onCancel, className = '', autoConfirm = true }: ProductSearchSelectProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [selectedProduct, setSelectedProduct] = useState<ProductListItem | null>(null)
  const [selectedVariant, setSelectedVariant] = useState<Variant | null>(null)
  const [isOpen, setIsOpen] = useState(false)
  const [dropdownStyle, setDropdownStyle] = useState<React.CSSProperties>({})
  
  // Keyboard navigation states
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const [variantHighlightedIndex, setVariantHighlightedIndex] = useState(-1)
  
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)
  const listRef = useRef<HTMLDivElement>(null)
  const variantContainerRef = useRef<HTMLDivElement>(null)
  const portalRef = useRef<HTMLDivElement>(null)

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search)
    }, 300)
    return () => clearTimeout(timer)
  }, [search])

  // Reset highlighted index when search results change
  useEffect(() => {
    setHighlightedIndex(-1)
  }, [debouncedSearch])

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

  const activeVariants = productDetail?.variants?.filter(v => v.isActive) || []
  const hasActiveVariants = activeVariants.length > 0
  const canConfirm = selectedProduct && (!hasActiveVariants || selectedVariant)

  // Auto-select single variant
  useEffect(() => {
    if (activeVariants.length === 1 && !selectedVariant && selectedProduct) {
      setSelectedVariant(activeVariants[0])
      setVariantHighlightedIndex(0)
    }
  }, [activeVariants, selectedVariant, selectedProduct])

  // Auto-confirm after variant selection (if enabled)
  useEffect(() => {
    if (autoConfirm && selectedVariant && selectedProduct && canConfirm) {
      // Small delay to show the selection visually
      const timer = setTimeout(() => {
        handleConfirm()
      }, 150)
      return () => clearTimeout(timer)
    }
  }, [selectedVariant, autoConfirm, selectedProduct, canConfirm])

  // Auto-confirm for products without variants
  useEffect(() => {
    if (autoConfirm && selectedProduct && productDetail && !hasActiveVariants) {
      const timer = setTimeout(() => {
        handleConfirm()
      }, 150)
      return () => clearTimeout(timer)
    }
  }, [selectedProduct, productDetail, hasActiveVariants, autoConfirm])

  // Position dropdown in portal to avoid clipping inside modals
  const updateDropdownPosition = useCallback(() => {
    if (!inputRef.current) return
    const rect = inputRef.current.getBoundingClientRect()
    setDropdownStyle({
      position: 'fixed',
      top: rect.bottom + 4,
      left: rect.left,
      width: rect.width,
      zIndex: 1000,
    })
  }, [])

  useLayoutEffect(() => {
    if (isOpen) {
      updateDropdownPosition()
    }
  }, [isOpen, updateDropdownPosition])

  useEffect(() => {
    if (!isOpen) return
    const handle = () => updateDropdownPosition()
    window.addEventListener('resize', handle)
    window.addEventListener('scroll', handle, true)
    return () => {
      window.removeEventListener('resize', handle)
      window.removeEventListener('scroll', handle, true)
    }
  }, [isOpen, updateDropdownPosition])

  // Close dropdown on outside click (including portal content)
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      const target = e.target as Node
      const insideTrigger = dropdownRef.current?.contains(target)
      const insidePortal = portalRef.current?.contains(target)
      if (!insideTrigger && !insidePortal) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  // Scroll highlighted item into view
  useEffect(() => {
    if (highlightedIndex >= 0 && listRef.current) {
      const items = listRef.current.querySelectorAll('[data-product-item]')
      const item = items[highlightedIndex] as HTMLElement
      if (item) {
        item.scrollIntoView({ block: 'nearest', behavior: 'smooth' })
      }
    }
  }, [highlightedIndex])

  const handleSelectProduct = useCallback((product: ProductListItem) => {
    setSelectedProduct(product)
    setSelectedVariant(null)
    setSearch('')
    setIsOpen(false)
    setHighlightedIndex(-1)
    setVariantHighlightedIndex(-1)
  }, [])

  const handleSelectVariant = useCallback((variant: Variant) => {
    setSelectedVariant(variant)
  }, [])

  const handleConfirm = useCallback(() => {
    if (!selectedProduct) return

    // If product has active variants but none selected, don't allow confirm
    if (hasActiveVariants && !selectedVariant) {
      return
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
  }, [selectedProduct, selectedVariant, hasActiveVariants, onSelect])

  const handleClear = useCallback(() => {
    setSelectedProduct(null)
    setSelectedVariant(null)
    setSearch('')
    setHighlightedIndex(-1)
    setVariantHighlightedIndex(-1)
    inputRef.current?.focus()
  }, [])

  // Keyboard navigation for products
  const handleSearchKeyDown = useCallback((e: React.KeyboardEvent<HTMLInputElement>) => {
    const items = searchResult?.items || []
    
    if (!isOpen || items.length === 0) {
      if (e.key === 'Escape' && onCancel) {
        onCancel()
      }
      return
    }

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault()
        setHighlightedIndex(prev => 
          prev < items.length - 1 ? prev + 1 : 0
        )
        break
      case 'ArrowUp':
        e.preventDefault()
        setHighlightedIndex(prev => 
          prev > 0 ? prev - 1 : items.length - 1
        )
        break
      case 'Enter':
        e.preventDefault()
        if (highlightedIndex >= 0 && items[highlightedIndex]) {
          handleSelectProduct(items[highlightedIndex])
        } else if (items.length === 1) {
          // If only one result, select it
          handleSelectProduct(items[0])
        }
        break
      case 'Escape':
        e.preventDefault()
        setIsOpen(false)
        setHighlightedIndex(-1)
        break
    }
  }, [isOpen, searchResult, highlightedIndex, handleSelectProduct, onCancel])

  // Keyboard navigation for variants
  const handleVariantKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (activeVariants.length === 0) return

    switch (e.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        e.preventDefault()
        setVariantHighlightedIndex(prev => 
          prev < activeVariants.length - 1 ? prev + 1 : 0
        )
        break
      case 'ArrowLeft':
      case 'ArrowUp':
        e.preventDefault()
        setVariantHighlightedIndex(prev => 
          prev > 0 ? prev - 1 : activeVariants.length - 1
        )
        break
      case 'Enter':
        e.preventDefault()
        if (variantHighlightedIndex >= 0 && activeVariants[variantHighlightedIndex]) {
          handleSelectVariant(activeVariants[variantHighlightedIndex])
        }
        break
      case 'Escape':
        e.preventDefault()
        handleClear()
        break
    }
  }, [activeVariants, variantHighlightedIndex, handleSelectVariant, handleClear])

  // Focus variant container when product is selected and has variants
  useEffect(() => {
    if (selectedProduct && hasActiveVariants && !loadingDetail && variantContainerRef.current) {
      variantContainerRef.current.focus()
    }
  }, [selectedProduct, hasActiveVariants, loadingDetail])

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
              onKeyDown={handleSearchKeyDown}
              placeholder="نام یا کد محصول را وارد کنید..."
              className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              autoComplete="off"
              autoFocus
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

          {/* Keyboard hint */}
          {isOpen && debouncedSearch.length >= 2 && searchResult && searchResult.items.length > 0 && (
            <div className="mt-1 flex items-center gap-2 text-xs text-slate-400">
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">↑↓</kbd>
              <span>پیمایش</span>
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">Enter</kbd>
              <span>انتخاب</span>
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">Esc</kbd>
              <span>بستن</span>
            </div>
          )}

          {/* Search Results Dropdown */}
          {isOpen && debouncedSearch.length >= 2 &&
            createPortal(
              <div
                ref={portalRef}
                style={dropdownStyle}
                className="rounded-lg border border-slate-200 bg-white shadow-lg"
              >
                {searching ? (
                  <div className="flex items-center justify-center py-8">
                    <Spinner className="h-6 w-6 text-emerald-600" />
                  </div>
                ) : !searchResult || searchResult.items.length === 0 ? (
                  <div className="py-6 text-center text-sm text-slate-500">
                    محصولی یافت نشد
                  </div>
                ) : (
                  <div className="max-h-64 overflow-y-auto overscroll-contain" ref={listRef}>
                    {searchResult.items.map((product, index) => (
                      <button
                        key={product.id}
                        type="button"
                        data-product-item
                        onClick={() => handleSelectProduct(product)}
                        className={`flex w-full items-center gap-3 px-4 py-3 text-right transition-colors ${
                          highlightedIndex === index
                            ? 'bg-emerald-50 border-r-2 border-emerald-500'
                            : 'hover:bg-slate-50'
                        }`}
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
                        {highlightedIndex === index && (
                          <div className="flex-shrink-0">
                            <kbd className="rounded bg-emerald-100 px-1.5 py-0.5 font-mono text-[10px] text-emerald-700">Enter</kbd>
                          </div>
                        )}
                      </button>
                    ))}
                  </div>
                )}
              </div>,
              document.body
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
              title="تغییر محصول (Esc)"
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
            <div
              ref={variantContainerRef}
              tabIndex={0}
              onKeyDown={handleVariantKeyDown}
              className="focus:outline-none"
            >
              <label className="mb-2 block text-sm font-medium text-slate-700">
                انتخاب واریانت <span className="text-red-500">*</span>
                {activeVariants.length === 1 && (
                  <span className="mr-2 text-xs text-emerald-600">(خودکار انتخاب شد)</span>
                )}
              </label>
              
              {/* Keyboard hint for variants */}
              <div className="mb-2 flex items-center gap-2 text-xs text-slate-400">
                <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">←→</kbd>
                <span>پیمایش</span>
                <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">Enter</kbd>
                <span>انتخاب</span>
              </div>
              
              <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                {activeVariants.map((variant, index) => (
                  <button
                    key={variant.id}
                    type="button"
                    onClick={() => handleSelectVariant(variant)}
                    className={`rounded-lg border-2 px-3 py-2.5 text-sm font-medium transition-all ${
                      selectedVariant?.id === variant.id
                        ? 'border-emerald-500 bg-emerald-50 text-emerald-700 ring-2 ring-emerald-500/20'
                        : variantHighlightedIndex === index
                        ? 'border-emerald-300 bg-emerald-50/50 text-emerald-600'
                        : 'border-slate-200 bg-white text-slate-600 hover:border-slate-300 hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span>{variant.value}</span>
                      {variantHighlightedIndex === index && selectedVariant?.id !== variant.id && (
                        <kbd className="rounded bg-emerald-100 px-1 py-0.5 font-mono text-[9px] text-emerald-700">↵</kbd>
                      )}
                    </div>
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

      {/* Actions - Only show if autoConfirm is disabled */}
      {!autoConfirm && (
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
      )}
    </div>
  )
}

