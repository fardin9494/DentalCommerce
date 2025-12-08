import { useState, useEffect, useRef } from 'react'
import { useStockProducts } from '@/features/stock-items/queries'
import { Spinner } from './Spinner'
import type { StockProduct } from '@/features/stock-items/api'

export interface StockProductSelection {
  productId: string
  productName: string
  variantId?: string
  variantValue?: string
  sku: string
}

interface StockProductSelectProps {
  warehouseId: string
  onSelect: (selection: StockProductSelection) => void
  onCancel?: () => void
  className?: string
}

export function StockProductSelect({
  warehouseId,
  onSelect,
  onCancel,
  className = '',
}: StockProductSelectProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [isOpen, setIsOpen] = useState(false)
  const [page, setPage] = useState(1)
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1) // Reset to page 1 on search change
    }, 300)
    return () => clearTimeout(timer)
  }, [search])

  // Search stock products
  const { data: searchResult, isLoading: searching } = useStockProducts({
    warehouseId,
    search: debouncedSearch,
    page,
    pageSize: 15,
  })

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node) &&
        inputRef.current &&
        !inputRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  function handleSelect(product: StockProduct) {
    onSelect({
      productId: product.productId,
      productName: product.productName || 'نامشخص',
      variantId: product.variantId || undefined,
      variantValue: product.variantValue || undefined,
      sku: product.sku,
    })
    setSearch('')
    setIsOpen(false)
  }

  function handleInputFocus() {
    setIsOpen(true)
  }

  function handleInputChange(e: React.ChangeEvent<HTMLInputElement>) {
    setSearch(e.target.value)
    setIsOpen(true)
  }

  const products = searchResult?.items || []

  return (
    <div className={`relative ${className}`}>
      {/* Search Input */}
      <div className="relative">
        <input
          ref={inputRef}
          type="text"
          value={search}
          onChange={handleInputChange}
          onFocus={handleInputFocus}
          placeholder="جستجوی محصول موجود در انبار..."
          className="w-full rounded-lg border border-slate-300 bg-white py-2.5 pl-10 pr-4 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
        />
        <svg
          className="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
          fill="none"
          viewBox="0 0 24 24"
          strokeWidth={2}
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z"
          />
        </svg>
        {searching && (
          <div className="absolute right-3 top-1/2 -translate-y-1/2">
            <Spinner className="h-4 w-4 text-emerald-600" />
          </div>
        )}
      </div>

      {/* Dropdown */}
      {isOpen && (
        <div
          ref={dropdownRef}
          className="absolute z-50 mt-2 w-full rounded-lg border border-slate-200 bg-white shadow-lg max-h-96 overflow-y-auto"
        >
          {searching && products.length === 0 ? (
            <div className="flex items-center justify-center py-8">
              <Spinner className="h-5 w-5 text-emerald-600" />
            </div>
          ) : products.length === 0 ? (
            <div className="py-8 text-center text-sm text-slate-500">
              {search ? 'محصولی یافت نشد' : 'برای جستجو شروع به تایپ کنید'}
            </div>
          ) : (
            <>
              <div className="divide-y divide-slate-100">
                {products.map((product) => (
                  <button
                    key={`${product.productId}-${product.variantId || 'no-variant'}`}
                    type="button"
                    onClick={() => handleSelect(product)}
                    className="w-full px-4 py-3 text-right transition-colors hover:bg-slate-50"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="font-medium text-slate-900">
                          {product.productName || 'نامشخص'}
                          {product.variantValue && (
                            <span className="text-emerald-600"> - {product.variantValue}</span>
                          )}
                        </div>
                        <div className="mt-0.5 flex items-center gap-2 text-xs text-slate-500">
                          <span className="font-mono">SKU: {product.sku}</span>
                          <span>•</span>
                          <span>
                            موجودی: {product.totalOnHand.toLocaleString('fa-IR')} (آزاد:{' '}
                            {product.totalAvailable.toLocaleString('fa-IR')})
                          </span>
                        </div>
                      </div>
                    </div>
                  </button>
                ))}
              </div>

              {/* Pagination */}
              {searchResult && searchResult.totalPages > 1 && (
                <div className="border-t border-slate-200 bg-slate-50 px-4 py-2">
                  <div className="flex items-center justify-between text-xs text-slate-600">
                    <span>
                      صفحه {page.toLocaleString('fa-IR')} از {searchResult.totalPages.toLocaleString('fa-IR')}
                    </span>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        disabled={page <= 1}
                        className="rounded px-2 py-1 transition-colors hover:bg-slate-200 disabled:opacity-50"
                      >
                        قبلی
                      </button>
                      <button
                        onClick={() => setPage((p) => Math.min(searchResult.totalPages, p + 1))}
                        disabled={page >= searchResult.totalPages}
                        className="rounded px-2 py-1 transition-colors hover:bg-slate-200 disabled:opacity-50"
                      >
                        بعدی
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  )
}

