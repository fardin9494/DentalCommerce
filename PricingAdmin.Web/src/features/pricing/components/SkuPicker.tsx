import { useCallback, useEffect, useMemo, useState } from 'react'
import { getCatalogProduct, searchCatalogProducts } from '../catalogApi'
import type { CatalogProductDetail, CatalogProductListItem, CatalogProductVariant } from '../catalogTypes'
import { getApiErrorMessage } from '../api'
import { setSkuLabel, formatSkuLabel } from '../catalogSkuLabels'

type PickedSku = {
  skuId: string
  label: string
}

export function SkuPicker({
  label,
  value,
  onChange,
  placeholder,
  disabled,
  showLabel = true,
}: {
  label: string
  value: string
  onChange: (next: PickedSku) => void
  placeholder?: string
  disabled?: boolean
  showLabel?: boolean
}) {
  const [term, setTerm] = useState('')
  const [searching, setSearching] = useState(false)
  const [results, setResults] = useState<CatalogProductListItem[]>([])
  const [searchError, setSearchError] = useState<string | null>(null)
  const [selectedProduct, setSelectedProduct] = useState<CatalogProductDetail | null>(null)
  const [loadingProduct, setLoadingProduct] = useState(false)
  const [selectedVariantSku, setSelectedVariantSku] = useState<string>('')

  const currentLabel = useMemo(() => formatSkuLabel(value), [value])

  const resetSelection = () => {
    setSelectedProduct(null)
    setSelectedVariantSku('')
  }

  const runSearch = useCallback(async (q: string) => {
    const normalized = q.trim()
    if (!normalized) {
      setResults([])
      setSearchError(null)
      return
    }
    if (normalized.length < 2) {
      setResults([])
      setSearchError('حداقل ۲ حرف وارد کنید.')
      return
    }

    setSearching(true)
    setSearchError(null)
    try {
      const res = await searchCatalogProducts({ search: normalized, page: 1, pageSize: 20 })
      setResults(res.items || [])
      setSearchError((res.items || []).length === 0 ? 'موردی یافت نشد.' : null)
    } catch (err) {
      const msg = getApiErrorMessage(err, 'خطا در جستجوی محصول.')
      setResults([])
      setSearchError(msg)
    } finally {
      setSearching(false)
    }
  }, [])

  useEffect(() => {
    if (!term.trim()) {
      setResults([])
      setSearchError(null)
      return
    }
    if (term.trim().length < 2) {
      setResults([])
      setSearchError('حداقل ۲ حرف وارد کنید.')
      return
    }
    const handle = window.setTimeout(() => runSearch(term), 350)
    return () => window.clearTimeout(handle)
  }, [runSearch, term])

  const selectProduct = async (id: string) => {
    setLoadingProduct(true)
    try {
      const product = await getCatalogProduct(id)
      setSelectedProduct(product)
      setResults([])
      setSearchError(null)

      const variants = getPricingVariants(product)
      if (variants.length > 0) {
        setSelectedVariantSku(variants[0].sku)
      } else {
        setSelectedVariantSku('')
      }
    } catch (err) {
      setSearchError(getApiErrorMessage(err, 'خطا در دریافت محصول.'))
    } finally {
      setLoadingProduct(false)
    }
  }

  const variants = useMemo(() => getPricingVariants(selectedProduct), [selectedProduct])
  const selectedVariant = useMemo(() => {
    if (!selectedProduct) return null
    if (variants.length === 0) return null
    if (!selectedVariantSku) return null
    return variants.find(v => v.sku === selectedVariantSku) ?? variants[0]
  }, [selectedProduct, selectedVariantSku, variants])

  const confirmVariant = () => {
    if (!selectedProduct) return
    const variant = selectedVariant
    if (!variant?.sku) {
      setSearchError('برای این محصول SKU قابل استفاده یافت نشد.')
      return
    }

    const labelParts = [selectedProduct.name]
    if (variant.value && variant.value !== 'محصول اصلی') labelParts.push(variant.value)
    const finalLabel = labelParts.join(' - ')

    setSkuLabel(variant.sku, finalLabel)
    onChange({ skuId: variant.sku, label: finalLabel })
    resetSelection()
    setTerm('')
  }

  return (
    <div className="space-y-2">
      {showLabel ? <label className="label">{label}</label> : null}

      {value ? (
        <div className="border rounded-md p-2 text-sm flex items-start justify-between gap-2">
          <div className="min-w-0">
            <div className="font-semibold truncate">{currentLabel ?? value}</div>
            {currentLabel ? <div className="text-xs text-gray-600 truncate">SKU: {value}</div> : null}
          </div>
          <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={() => onChange({ skuId: '', label: '' })} disabled={disabled}>
            پاک کردن
          </button>
        </div>
      ) : null}

      <input
        className="input"
        value={term}
        onChange={e => {
          setTerm(e.target.value)
          if (selectedProduct) resetSelection()
        }}
        placeholder={placeholder ?? 'جستجوی محصول (حداقل ۲ حرف)'}
        disabled={disabled}
      />

      {searching ? <div className="text-xs text-gray-500">در حال جستجو...</div> : null}
      {loadingProduct ? <div className="text-xs text-gray-500">در حال دریافت محصول...</div> : null}
      {searchError ? <div className="text-xs text-red-600">{searchError}</div> : null}

      {results.length > 0 && (
        <div className="border rounded-md overflow-hidden">
          <table className="min-w-full text-sm text-center">
            <thead>
              <tr className="bg-gray-100">
                <th className="p-2">نام</th>
                <th className="p-2">کد</th>
                <th className="p-2">عملیات</th>
              </tr>
            </thead>
            <tbody>
              {results.map(p => (
                <tr key={p.id} className="border-b last:border-0">
                  <td className="p-2">{p.name}</td>
                  <td className="p-2">{p.code || '-'}</td>
                  <td className="p-2">
                    <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={() => selectProduct(p.id)}>
                      انتخاب
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {selectedProduct && variants.length > 0 && (
        <div className="border rounded-md p-3 space-y-2">
          <div className="text-sm font-semibold">{selectedProduct.name}</div>
          <div>
            <label className="label">واریانت</label>
            <select className="input" value={selectedVariantSku} onChange={e => setSelectedVariantSku(e.target.value)}>
              {variants.map(v => (
                <option key={v.id} value={v.sku}>
                  {v.value || v.sku}
                </option>
              ))}
            </select>
          </div>
          <div className="flex items-center gap-2">
            <button type="button" className="btn" onClick={confirmVariant}>تایید</button>
            <button type="button" className="btn-secondary px-3 py-2 rounded" onClick={resetSelection}>انصراف</button>
          </div>
        </div>
      )}
    </div>
  )
}

function getPricingVariants(product: CatalogProductDetail | null): CatalogProductVariant[] {
  if (!product) return []
  const variants = (product.variants || []).filter(v => v.sku)
  if (variants.length > 0) return variants
  const sku = product.code?.trim()
  if (!sku) return []
  return [
    {
      id: `${product.id}-base`,
      sku,
      value: 'محصول اصلی',
      isActive: true,
    },
  ]
}
