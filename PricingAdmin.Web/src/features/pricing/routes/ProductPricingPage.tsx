import { useCallback, useEffect, useMemo, useRef, useState, type Dispatch, type SetStateAction } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { bulkUpdatePrices, createCampaign, createOverride, deleteCampaign, getApiErrorMessage, listOverrides, updateCampaign, updateOverride } from '../api'
import { searchCatalogProducts, getCatalogProduct, listCatalogStores } from '../catalogApi'
import type { CatalogProductDetail, CatalogProductListItem, CatalogProductStore, CatalogProductVariant, CatalogStoreListItem } from '../catalogTypes'
import type { BenefitDefinition, Guardrails, OverrideType, PriceList, PromotionCampaign, StackingMode } from '../types'
import { useCampaigns, useCreatePriceList, usePriceList, usePriceLists } from '../queries'
import { BenefitBuilder, buildBenefit, makeDefaultBenefit, type BenefitBuilderState } from '../components/RuleBuilders'

const stackingOptions: Array<{ value: StackingMode; label: string; help: string }> = [
  { value: 'BestOfEachGroup', label: 'بهترین هر گروه', help: 'در هر گروه فقط بهترین کمپین انتخاب می شود.' },
  { value: 'BestPrice', label: 'بهترین قیمت', help: 'فقط کمپینی که بیشترین تخفیف می دهد اعمال می شود.' },
  { value: 'PriorityOnly', label: 'فقط اولویت', help: 'فقط کمپین با بالاترین اولویت اعمال می شود.' },
  { value: 'Cascading', label: 'آبشاری', help: 'کمپین ها به ترتیب اولویت روی هم اعمال می شوند.' },
]

type SiteOverrideState = {
  id?: string
  overrideType: OverrideType
  value: string
}

type DiscountState = {
  name: string
  isActive: boolean
  validFrom: string
  validTo: string
  priority: string
  stackingGroup: string
  stackingMode: StackingMode
  combinableWithOtherPromotions: boolean
  combinableWithCoupons: boolean
  exclusiveGroup: string
  maxDiscountPercent: string
  maxDiscountAmount: string
  benefit: BenefitBuilderState
}

type VariantPricingState = {
  skuId: string
  variantLabel: string
  basePrice: string
  siteOverrides: Record<string, SiteOverrideState>
  discount: DiscountState
  campaignId?: string
  campaignCount: number
}

export function ProductPricingPage() {
  const [search, setSearch] = useState('')
  const [searching, setSearching] = useState(false)
  const [searchResults, setSearchResults] = useState<CatalogProductListItem[]>([])
  const [searchError, setSearchError] = useState<string | null>(null)
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const searchBoxRef = useRef<HTMLDivElement>(null)
  const [selectedProduct, setSelectedProduct] = useState<CatalogProductDetail | null>(null)
  const [loadingProduct, setLoadingProduct] = useState(false)
  const [selectedVariantSku, setSelectedVariantSku] = useState<string | null>(null)
  const [variantsState, setVariantsState] = useState<Record<string, VariantPricingState>>({})
  const [initKey, setInitKey] = useState<string | null>(null)
  const [loadingOverrides, setLoadingOverrides] = useState(false)
  const [savingBaseSku, setSavingBaseSku] = useState<string | null>(null)
  const [savingSitesSku, setSavingSitesSku] = useState<string | null>(null)
  const [savingDiscountSku, setSavingDiscountSku] = useState<string | null>(null)
  const [autoCreatingList, setAutoCreatingList] = useState(false)
  const [autoListError, setAutoListError] = useState<string | null>(null)
  const [catalogStores, setCatalogStores] = useState<CatalogStoreListItem[]>([])

  const { data: priceLists } = usePriceLists()
  const activePriceList = useMemo(() => pickActivePriceList(priceLists), [priceLists])
  const { data: priceList } = usePriceList(activePriceList?.id)
  const currency = priceList?.currency || 'IRR'

  const { data: campaigns } = useCampaigns()
  const createPriceList = useCreatePriceList()
  const campaignIndex = useMemo(() => buildCampaignIndex(campaigns ?? []), [campaigns])
  const pricingVariants = useMemo(() => getPricingVariants(selectedProduct), [selectedProduct])
  const selectedVariant = useMemo(() => {
    if (pricingVariants.length === 0) return null
    if (!selectedVariantSku) return null
    return pricingVariants.find(v => normalizeSku(v.sku) === normalizeSku(selectedVariantSku)) ?? pricingVariants[0]
  }, [pricingVariants, selectedVariantSku])

  const runSearch = useCallback(async (term: string, showToast = false) => {
    const normalized = term.trim()
    if (!normalized) {
      setSearchResults([])
      setSearchError(null)
      return
    }
    if (normalized.length < 2) {
      setSearchResults([])
      setSearchError('برای جستجو حداقل ۲ حرف وارد کنید.')
      return
    }
    setSearching(true)
    setSearchError(null)
    try {
      const res = await searchCatalogProducts({ search: normalized, page: 1, pageSize: 20 })
      setSearchResults(res.items || [])
      setSearchError((res.items || []).length === 0 ? 'موردی یافت نشد.' : null)
    } catch (err) {
      const msg = getApiErrorMessage(err, 'خطا در جستجوی محصولات. ارتباط با کاتالوگ برقرار نشد.')
      setSearchResults([])
      setSearchError(msg)
      if (showToast) swalToastError(msg)
    } finally {
      setSearching(false)
    }
  }, [])

  const handleSearch = async () => {
    await runSearch(search, true)
  }

  const handleSelectProduct = async (id: string) => {
    setLoadingProduct(true)
    try {
      const product = await getCatalogProduct(id)
      setSelectedProduct(product)
      setSelectedVariantSku(null)
      setSearchResults([])
      setSearchError(null)
      setIsSearchOpen(false)
      setHighlightedIndex(-1)
      setInitKey(null)
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در دریافت محصول.'))
    } finally {
      setLoadingProduct(false)
    }
  }
  const createDefaultPriceList = useCallback(async () => {
    if (autoCreatingList) return
    setAutoCreatingList(true)
    setAutoListError(null)
    try {
      const now = new Date().toISOString()
      await createPriceList.mutateAsync({
        name: 'لیست قیمت اصلی',
        currency: 'IRR',
        validFrom: now,
        validTo: null,
        isActive: true,
      })
      swalToastSuccess('لیست قیمت ایجاد شد.')
    } catch (err) {
      const msg = getApiErrorMessage(err, 'خطا در ایجاد لیست قیمت.')
      setAutoListError(msg)
      swalToastError(msg)
    } finally {
      setAutoCreatingList(false)
    }
  }, [autoCreatingList, createPriceList])

  useEffect(() => {
    const term = search.trim()
    if (!term) {
      setSearchResults([])
      setSearchError(null)
      return
    }
    if (term.length < 2) {
      setSearchResults([])
      setSearchError('برای جستجو حداقل ۲ حرف وارد کنید.')
      return
    }
    const handle = window.setTimeout(() => {
      runSearch(term)
    }, 350)
    return () => window.clearTimeout(handle)
  }, [runSearch, search])

  useEffect(() => {
    const onMouseDown = (e: MouseEvent) => {
      const target = e.target as Node
      if (!searchBoxRef.current?.contains(target)) {
        setIsSearchOpen(false)
        setHighlightedIndex(-1)
      }
    }
    document.addEventListener('mousedown', onMouseDown)
    return () => document.removeEventListener('mousedown', onMouseDown)
  }, [])

  useEffect(() => {
    let active = true
    const loadStores = async () => {
      try {
        const stores = await listCatalogStores()
        if (!active) return
        setCatalogStores(stores || [])
      } catch (err) {
        if (!active) return
        swalToastError(getApiErrorMessage(err, 'خطا در دریافت لیست سایت‌ها از کاتالوگ.'))
      }
    }
    void loadStores()
    return () => {
      active = false
    }
  }, [])
  useEffect(() => {
    if (!selectedProduct) return
    if (!priceLists) return
    if (priceLists.length > 0) return
    if (autoCreatingList || autoListError) return
    createDefaultPriceList()
  }, [selectedProduct, priceLists, autoCreatingList, autoListError, createDefaultPriceList])

  useEffect(() => {
    if (pricingVariants.length === 0) {
      setSelectedVariantSku(null)
      return
    }
    if (!selectedVariantSku || !pricingVariants.some(v => normalizeSku(v.sku) === normalizeSku(selectedVariantSku))) {
      setSelectedVariantSku(pricingVariants[0].sku)
    }
  }, [pricingVariants, selectedVariantSku])

  useEffect(() => {
    if (!selectedProduct || !priceList) return
    const key = `${selectedProduct.id}:${priceList.id}`
    if (initKey === key) return
    setInitKey(key)

    const variantRows = pricingVariants
    const storeRows = selectedProduct.stores || []
    const initState: Record<string, VariantPricingState> = {}

    for (const variant of variantRows) {
      const skuKey = normalizeSku(variant.sku)
      const basePrice = findBasePrice(priceList, variant.sku)
      const siteOverrides: Record<string, SiteOverrideState> = {}
      for (const store of storeRows) {
        siteOverrides[store.storeId] = {
          overrideType: 'FixedPrice',
          value: '',
        }
      }

      const campaignForSku = campaignIndex.single.get(skuKey)
      const campaignCount = campaignIndex.counts.get(skuKey) ?? 0
      const discount = campaignForSku
        ? mapCampaignToDiscountState(campaignForSku)
        : makeDiscountState(currency, selectedProduct.name, variant.value || variant.sku)

      initState[skuKey] = {
        skuId: variant.sku,
        variantLabel: variant.value || variant.sku,
        basePrice,
        siteOverrides,
        discount,
        campaignId: campaignForSku?.id,
        campaignCount,
      }
    }

    setVariantsState(initState)
  }, [selectedProduct, priceList, initKey, campaignIndex, currency])

  useEffect(() => {
    if (!selectedProduct) return
    const skus = pricingVariants.map(v => v.sku)
    const storeIds = new Set((selectedProduct.stores || []).map(s => s.storeId))
    if (skus.length === 0) return

    setLoadingOverrides(true)
    Promise.all(skus.map(sku => listOverrides({ skuId: sku })))
      .then((results) => {
        setVariantsState(prev => {
          const next = { ...prev }
          results.forEach((overrides, idx) => {
            const sku = skus[idx]
            const skuKey = normalizeSku(sku)
            const current = next[skuKey]
            if (!current) return
            const siteOverrides = { ...current.siteOverrides }

            overrides
              .filter(o => o.scopeType === 'Site' && storeIds.has(o.scopeId))
              .forEach(o => {
                siteOverrides[o.scopeId] = {
                  id: o.id,
                  overrideType: o.overrideType,
                  value: String(o.value ?? ''),
                }
              })

            next[skuKey] = { ...current, siteOverrides }
          })
          return next
        })
      })
      .catch((err) => {
        swalToastError(getApiErrorMessage(err, 'خطا در دریافت قیمت های سایت.'))
      })
      .finally(() => setLoadingOverrides(false))
  }, [selectedProduct])

  useEffect(() => {
    if (!selectedProduct || !campaigns || campaigns.length === 0) return
    setVariantsState(prev => {
      const next = { ...prev }
      for (const variant of pricingVariants) {
        if (!variant.sku) continue
        const skuKey = normalizeSku(variant.sku)
        const current = next[skuKey]
        if (!current || current.campaignId) continue
        const campaignForSku = campaignIndex.single.get(skuKey)
        if (!campaignForSku) continue
        next[skuKey] = {
          ...current,
          discount: mapCampaignToDiscountState(campaignForSku),
          campaignId: campaignForSku.id,
          campaignCount: campaignIndex.counts.get(skuKey) ?? current.campaignCount,
        }
      }
      return next
    })
  }, [campaigns, campaignIndex, selectedProduct])

  const productStores = selectedProduct?.stores || []
  const storeNameMap = useMemo(() => new Map(catalogStores.map(s => [s.id, s.name])), [catalogStores])
  const variants = pricingVariants

  return (
    <div className="space-y-4">
      <PageHeader title="قیمت گذاری محصول">
        محصول را از کاتالوگ جستجو کنید و قیمت پایه، قیمت سایت و تخفیف را برای هر واریانت تنظیم کنید.
      </PageHeader>
      <details className="card p-4" open>
        <summary className="cursor-pointer font-semibold">راهنمای سریع این صفحه</summary>
        <div className="mt-3 text-sm text-gray-700 space-y-2">
          <div>1) محصول را جستجو و انتخاب کنید. اگر محصول واریانت دارد، قیمت فقط برای واریانت‌ها ثبت می‌شود.</div>
          <div>2) «قیمت پایه (Global)» را ثبت کنید (این قیمت در لیست قیمت فعال ذخیره می‌شود).</div>
          <div>3) در بخش «قیمت‌های ویژه در سایت‌ها»، برای هر سایت نوع تغییر (درصدی/ریالی/قیمت ثابت) و مقدار را وارد و ذخیره کنید.</div>
          <div>4) بخش «تخفیف ویژه برای کمپین» یک کمپین ساده مخصوص همین SKU می‌سازد/به‌روزرسانی می‌کند (برای قوانین پیچیده‌تر از صفحه «کمپین‌ها» استفاده کنید).</div>
          <div className="text-xs text-gray-600">برای دیدن خروجی نهایی و دلیل اعمال/رد شدن‌ها از «پیش‌نمایش قیمت» استفاده کنید.</div>
        </div>
      </details>

      <div className="card p-4 space-y-3" ref={searchBoxRef}>
        <div className="relative">
          <label className="label">جستجوی محصول (کاتالوگ)</label>
          <div className="relative">
            <input
              className="w-full rounded-lg border border-slate-300 py-2.5 pl-10 pr-4 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20"
              value={search}
              onChange={e => {
                setSearch(e.target.value)
                setIsSearchOpen(true)
                setHighlightedIndex(-1)
              }}
              onFocus={() => setIsSearchOpen(true)}
              onKeyDown={(e) => {
                const items = searchResults
                if (!isSearchOpen || items.length === 0) {
                  if (e.key === 'Escape') {
                    setIsSearchOpen(false)
                    setHighlightedIndex(-1)
                  }
                  return
                }
                switch (e.key) {
                  case 'ArrowDown':
                    e.preventDefault()
                    setHighlightedIndex(prev => (prev < items.length - 1 ? prev + 1 : 0))
                    break
                  case 'ArrowUp':
                    e.preventDefault()
                    setHighlightedIndex(prev => (prev > 0 ? prev - 1 : items.length - 1))
                    break
                  case 'Enter':
                    e.preventDefault()
                    if (highlightedIndex >= 0 && items[highlightedIndex]) {
                      handleSelectProduct(items[highlightedIndex].id)
                    } else if (items.length === 1) {
                      handleSelectProduct(items[0].id)
                    }
                    break
                  case 'Escape':
                    e.preventDefault()
                    setIsSearchOpen(false)
                    setHighlightedIndex(-1)
                    break
                }
              }}
              placeholder="حداقل ۲ حرف از نام محصول..."
              autoComplete="off"
            />
            <div className="absolute left-3 top-1/2 -translate-y-1/2">
              {searching ? (
                <div className="h-4 w-4 animate-spin rounded-full border-2 border-slate-300 border-t-emerald-600" />
              ) : (
                <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" />
                </svg>
              )}
            </div>
          </div>

          {isSearchOpen && search.trim().length >= 2 && (
            <div className="mt-1 flex items-center gap-2 text-xs text-slate-400">
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">↑/↓</kbd>
              <span>حرکت</span>
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">Enter</kbd>
              <span>انتخاب</span>
              <kbd className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px]">Esc</kbd>
              <span>بستن</span>
            </div>
          )}

          {isSearchOpen && search.trim().length >= 2 && (
            <div className="absolute z-20 mt-2 w-full rounded-lg border border-slate-200 bg-white shadow-lg">
              {searching ? (
                <div className="flex items-center justify-center py-8">
                  <div className="h-6 w-6 animate-spin rounded-full border-2 border-slate-300 border-t-emerald-600" />
                </div>
              ) : searchError ? (
                <div className="py-4 text-center text-sm text-red-600">{searchError}</div>
              ) : searchResults.length === 0 ? (
                <div className="py-6 text-center text-sm text-slate-500">موردی یافت نشد.</div>
              ) : (
                <div className="max-h-72 overflow-y-auto overscroll-contain">
                  {searchResults.map((p, index) => (
                    <button
                      key={p.id}
                      type="button"
                      onMouseEnter={() => setHighlightedIndex(index)}
                      onClick={() => handleSelectProduct(p.id)}
                      className={`flex w-full items-center gap-3 px-4 py-3 text-right transition-colors ${
                        highlightedIndex === index
                          ? 'bg-emerald-50 border-r-2 border-emerald-500'
                          : 'hover:bg-slate-50'
                      }`}
                    >
                      {p.mainImageUrl ? (
                        <img src={p.mainImageUrl} alt={p.name} className="h-10 w-10 rounded-lg object-cover" />
                      ) : (
                        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-slate-100 text-slate-400">
                          <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" d="M4 7V4a1 1 0 0 1 1-1h3M4 17v3a1 1 0 0 0 1 1h3M20 7V4a1 1 0 0 0-1-1h-3M20 17v3a1 1 0 0 1-1 1h-3M7 7h10M7 12h10M7 17h6" />
                          </svg>
                        </div>
                      )}
                      <div className="min-w-0 flex-1 text-right">
                        <div className="font-medium text-slate-900 truncate">{p.name}</div>
                        <div className="mt-0.5 text-xs text-slate-500 flex flex-wrap items-center gap-2 justify-end">
                          {p.code ? <span className="font-mono">{p.code}</span> : null}
                          {p.brandName ? <span className="text-slate-400">•</span> : null}
                          {p.brandName ? <span>{p.brandName}</span> : null}
                        </div>
                      </div>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </div>

        <div className="flex items-center justify-end gap-2">
          <button className="btn-secondary px-3 py-2 rounded" onClick={handleSearch} disabled={searching || search.trim().length < 2}>
            جستجو
          </button>
          <button
            className="btn-secondary px-3 py-2 rounded"
            type="button"
            onClick={() => {
              setSearch('')
              setSearchResults([])
              setSearchError(null)
              setIsSearchOpen(false)
              setHighlightedIndex(-1)
            }}
            disabled={!search && searchResults.length === 0}
          >
            پاک کردن
          </button>
        </div>
      </div>
      {loadingProduct && <Spinner />}

      {selectedProduct && (
        <div className="space-y-4">
          <div className="card p-4 space-y-2">
            <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
              <div>
                <div className="text-lg font-semibold">{selectedProduct.name}</div>
                <div className="text-xs text-gray-600">کد محصول: {selectedProduct.code || '-'} | برند: {selectedProduct.brandName || '-'}</div>
              </div>
              <div className="text-xs text-gray-600">
                لیست قیمت فعال: {activePriceList?.name || 'نامشخص'} ({currency})
              </div>
            </div>
            {!priceList && (
              <div className="text-sm text-amber-700 space-y-1">
                <div>{autoCreatingList ? 'در حال ایجاد لیست قیمت...' : 'لیست قیمت فعالی وجود ندارد.'}</div>
                {autoListError && (
                  <div className="text-xs text-red-600">
                    {autoListError}
                    <button type="button" className="btn-secondary px-2 py-1 rounded ml-2" onClick={createDefaultPriceList}>
                      ایجاد لیست قیمت
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>

          {variants.length > 1 && (
            <div className="card p-4 space-y-2">
              <label className="label">انتخاب واریانت</label>
              <div className="flex flex-wrap gap-2">
                {variants.map(variant => {
                  const isSelected = (selectedVariantSku || '').toLowerCase() === variant.sku.toLowerCase()
                  const baseCls = 'badge cursor-pointer select-none transition-colors'
                  const cls = isSelected
                    ? `${baseCls} badge-green`
                    : variant.isActive
                      ? `${baseCls} badge-gray hover:bg-gray-200`
                      : `${baseCls} badge-gray opacity-60`

                  return (
                    <button
                      key={variant.id}
                      type="button"
                      className={cls}
                      onClick={() => setSelectedVariantSku(variant.sku)}
                      title={variant.isActive ? '' : 'غیرفعال'}
                    >
                      {variant.value || variant.sku}
                    </button>
                  )
                })}
              </div>
              <div className="text-xs text-gray-600">برای هر واریانت قیمت جداگانه ثبت می‌شود.</div>
            </div>
          )}
          {variants.length === 0 && (
            <div className="card p-4 text-sm text-gray-600">برای این محصول SKU پیدا نشد. اگر محصول تک‌واریانت است، کد محصول را در کاتالوگ ثبت کنید.</div>
          )}
          {selectedVariant && (() => {
            const skuKey = normalizeSku(selectedVariant.sku)
            const state = variantsState[skuKey]
            if (!state) return null
            const stackingHelp = stackingOptions.find(o => o.value === state.discount.stackingMode)?.help

            return (
              <div key={selectedVariant.id} className="card p-4 space-y-4">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
                  <div>
                    <div className="font-semibold">{state.variantLabel}</div>
                    <div className="text-xs text-gray-600">SKU: {state.skuId}</div>
                  </div>
                  {selectedVariant.isActive ? null : <span className="badge badge-gray">غیرفعال</span>}
                </div>

                <details open className="border rounded-md p-3 space-y-3">
                  <summary className="cursor-pointer select-none font-semibold text-sm mb-3">قیمت پایه (Global)</summary>
                  <div className="flex flex-col md:flex-row gap-3 md:items-end">
                    <div className="flex-1">
                      <label className="label">قیمت پایه (Global)</label>
                      <input
                        className="input"
                        type="number"
                        value={state.basePrice}
                        onChange={e => updateBasePrice(skuKey, e.target.value, setVariantsState)}
                      />
                    </div>
                    <button
                      className="btn"
                      onClick={() => handleSaveBasePrice({ skuKey, state, priceList, setSavingBaseSku })}
                      disabled={!priceList || savingBaseSku === skuKey}
                    >
                      {savingBaseSku === skuKey ? 'در حال ذخیره...' : 'ذخیره قیمت پایه'}
                    </button>
                    <button
                      className="btn-secondary px-3 py-2 rounded"
                      onClick={() => applyGlobalToSites(skuKey, state, setVariantsState)}
                      disabled={!state.basePrice}
                    >
                      اعمال قیمت پایه به همه
                    </button>
                  </div>
                </details>

                <details open className="border rounded-md p-3 space-y-3">
                  <summary className="cursor-pointer select-none flex items-center justify-between">
                    <h4 className="font-semibold">قیمت‌های ویژه در سایت‌ها</h4>
                    {loadingOverrides && <span className="text-xs text-gray-500">در حال دریافت...</span>}
                  </summary>
                  {productStores.length === 0 ? (
                    <div className="text-sm text-gray-600">سایتی برای نمایش این محصول ثبت نشده است.</div>
                  ) : (
                    <div className="overflow-x-auto">
                      <table className="min-w-full text-sm text-center">
                        <thead>
                          <tr className="bg-gray-100">
                            <th className="p-2">سایت</th>
                            <th className="p-2">نوع تغییر</th>
                            <th className="p-2">مقدار</th>
                            <th className="p-2">قیمت نهایی</th>
                          </tr>
                        </thead>
                        <tbody>
                          {productStores.map(store => {
                            const override = state.siteOverrides[store.storeId]
                              const label = storeLabel(store, storeNameMap)
                            const type = (override?.overrideType || 'FixedPrice') as OverrideType
                            const base = parseNumberOrNull(state.basePrice)
                            const rawValue = parseNumberOrNull(override?.value)
                            const preview = computeOverridePreview(base, type, rawValue, currency)
                            const finalPrice = rawValue == null ? base : (preview ?? base)
                            return (
                              <tr key={store.storeId} className="border-b last:border-0">
                                <td className="p-2">{label}</td>
                                <td className="p-2">
                                  <select
                                    className="input"
                                    value={override?.overrideType || 'FixedPrice'}
                                    onChange={e => updateSiteOverrideType(skuKey, store.storeId, e.target.value as OverrideType, setVariantsState)}
                                  >
                                    <option value="PercentOff">درصدی کاهش</option>
                                    <option value="AmountOff">ریالی کاهش</option>
                                    <option value="FixedPrice">قیمت ثابت</option>
                                  </select>
                                </td>
                                <td className="p-2">
                                  <input
                                    className="input"
                                    type="number"
                                    value={override?.value || ''}
                                    placeholder={type === 'PercentOff' ? 'درصد' : type === 'AmountOff' ? `مبلغ (${currencyLabel(currency)})` : `قیمت (${currencyLabel(currency)})`}
                                    onChange={e => updateSiteOverrideValue(skuKey, store.storeId, e.target.value, setVariantsState)}
                                  />
                                </td>
                                <td className="p-2">
                                  <input
                                    className="input bg-gray-50"
                                    readOnly
                                    value={finalPrice == null ? '' : formatNumber(finalPrice)}
                                    placeholder={rawValue != null && preview == null && (type === 'PercentOff' || type === 'AmountOff') ? 'ابتدا قیمت پایه' : '—'}
                                  />
                                </td>
                              </tr>
                            )
                          })}
                        </tbody>
                      </table>
                    </div>
                  )}

                  <button
                    className="btn"
                    onClick={() => handleSaveSiteOverrides({ skuKey, state, productStores, currency, setSavingSitesSku, setVariantsState })}
                    disabled={savingSitesSku === skuKey}
                  >
                    {savingSitesSku === skuKey ? 'در حال ذخیره...' : 'ذخیره قیمت‌های سایت'}
                  </button>
                </details>

                <details className="border rounded-md p-3 space-y-3">
                  <summary className="cursor-pointer select-none flex items-center justify-between">
                    <h4 className="font-semibold">تخفیف ویژه برای کمپین</h4>
                    {state.campaignCount > 1 && (
                      <span className="text-xs text-amber-600">چند کمپین برای این SKU وجود دارد. لطفا در کمپین‌ها بررسی کنید.</span>
                    )}
                  </summary>

                  <div className="mt-3 space-y-3">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-3">
                      <div className="bg-gray-50 border rounded-md p-3 space-y-3">
                        <div className="font-semibold text-sm">اطلاعات اصلی</div>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <div>
                            <label className="label">نام تخفیف</label>
                            <input className="input" value={state.discount.name} onChange={e => updateDiscountField(skuKey, { name: e.target.value }, setVariantsState)} />
                          </div>
                          <div>
                            <label className="label">اولویت</label>
                            <input className="input" type="number" value={state.discount.priority} onChange={e => updateDiscountField(skuKey, { priority: e.target.value }, setVariantsState)} />
                          </div>
                        </div>
                        <label className="flex items-center gap-2 text-sm">
                          <input type="checkbox" checked={state.discount.isActive} onChange={e => updateDiscountField(skuKey, { isActive: e.target.checked }, setVariantsState)} />
                          کمپین فعال
                        </label>
                      </div>

                      <div className="bg-gray-50 border rounded-md p-3 space-y-3">
                        <div className="font-semibold text-sm">زمان‌بندی</div>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <div>
                            <label className="label">از تاریخ</label>
                            <JalaliDateTimePicker value={state.discount.validFrom} onChange={(v) => updateDiscountField(skuKey, { validFrom: v }, setVariantsState)} clearable />
                          </div>
                          <div>
                            <label className="label">تا تاریخ</label>
                            <JalaliDateTimePicker value={state.discount.validTo} onChange={(v) => updateDiscountField(skuKey, { validTo: v }, setVariantsState)} clearable />
                          </div>
                        </div>
                      </div>

                      <div className="bg-gray-50 border rounded-md p-3 space-y-3 lg:col-span-2">
                        <div className="font-semibold text-sm">تجمیع و انحصار</div>
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                          <div className="md:col-span-2">
                            <label className="label">گروه تجمیع</label>
                            <input className="input" value={state.discount.stackingGroup} onChange={e => updateDiscountField(skuKey, { stackingGroup: e.target.value }, setVariantsState)} />
                            <div className="flex flex-wrap gap-2 mt-2 text-xs">
                              {[
                                { value: 'Product', label: 'محصول' },
                                { value: 'Category', label: 'گروه کالا' },
                                { value: 'Seasonal', label: 'فصلی' },
                                { value: 'Bundle', label: 'باندل' },
                                { value: 'Cashback', label: 'کش‌بک' },
                                { value: 'Discount', label: 'عمومی' },
                              ].map(g => (
                                <button
                                  key={g.value}
                                  type="button"
                                  className="btn-secondary px-2 py-1 rounded"
                                  onClick={() => updateDiscountField(skuKey, { stackingGroup: g.value }, setVariantsState)}
                                >
                                  {g.label}
                                </button>
                              ))}
                            </div>
                          </div>
                          <div>
                            <label className="label">مدل تجمیع</label>
                            <select className="input" value={state.discount.stackingMode} onChange={e => updateDiscountField(skuKey, { stackingMode: e.target.value as StackingMode }, setVariantsState)}>
                              {stackingOptions.map(o => (
                                <option key={o.value} value={o.value}>{o.label}</option>
                              ))}
                            </select>
                          </div>
                          <div className="md:col-span-3">
                            <label className="label">گروه انحصار</label>
                            <input className="input" value={state.discount.exclusiveGroup} onChange={e => updateDiscountField(skuKey, { exclusiveGroup: e.target.value }, setVariantsState)} />
                            {stackingHelp ? <p className="text-xs text-gray-600 mt-2">{stackingHelp}</p> : null}
                          </div>
                        </div>
                      </div>

                      <div className="bg-gray-50 border rounded-md p-3 space-y-3 lg:col-span-2">
                        <div className="font-semibold text-sm">ترکیب و گاردریل</div>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <label className="flex items-center gap-2 text-sm">
                            <input type="checkbox" checked={state.discount.combinableWithOtherPromotions} onChange={e => updateDiscountField(skuKey, { combinableWithOtherPromotions: e.target.checked }, setVariantsState)} />
                            قابل ترکیب با تخفیف‌های دیگر
                          </label>
                          <label className="flex items-center gap-2 text-sm">
                            <input type="checkbox" checked={state.discount.combinableWithCoupons} onChange={e => updateDiscountField(skuKey, { combinableWithCoupons: e.target.checked }, setVariantsState)} />
                            قابل ترکیب با کد تخفیف
                          </label>
                        </div>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                          <div>
                            <label className="label">حداکثر درصد تخفیف</label>
                            <input className="input" type="number" value={state.discount.maxDiscountPercent} onChange={e => updateDiscountField(skuKey, { maxDiscountPercent: e.target.value }, setVariantsState)} />
                          </div>
                          <div>
                            <label className="label">حداکثر مبلغ تخفیف</label>
                            <input className="input" type="number" value={state.discount.maxDiscountAmount} onChange={e => updateDiscountField(skuKey, { maxDiscountAmount: e.target.value }, setVariantsState)} />
                          </div>
                        </div>
                      </div>
                    </div>

                    <div className="border rounded-md p-3 space-y-2">
                      <div className="flex items-center justify-between">
                        <h5 className="font-semibold">مزیت تخفیف</h5>
                        <div className="text-xs text-gray-600">
                          قیمت پایه فعلی: {(() => { const n = parseNumberOrNull(state.basePrice); return n == null ? '—' : `${formatNumber(n)} ${currencyLabel(currency)}` })()}
                        </div>
                      </div>
                      <BenefitBuilder value={state.discount.benefit} onChange={(next) => updateDiscountField(skuKey, { benefit: next }, setVariantsState)} />
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <button
                      className="btn flex-1"
                      onClick={() => handleSaveDiscount({ skuKey, state, setSavingDiscountSku, setVariantsState })}
                      disabled={savingDiscountSku === skuKey}
                    >
                      {savingDiscountSku === skuKey ? 'در حال ذخیره...' : state.campaignId ? 'به‌روزرسانی تخفیف' : 'ذخیره تخفیف'}
                    </button>
                    {state.campaignId && (
                      <button
                        type="button"
                        className="btn-red px-3 py-2 rounded"
                        onClick={() => handleDeleteDiscount({ skuKey, state, setVariantsState })}
                      >
                        حذف
                      </button>
                    )}
                  </div>
                </details>
              </div>
            )
          })()}
        </div>
      )}
    </div>
  )
}
function pickActivePriceList(lists?: PriceList[] | null) {
  if (!lists || lists.length === 0) return null
  const now = new Date()
  const active = lists.filter(l => {
    if (!l.isActive) return false
    const fromOk = !l.validFrom || new Date(l.validFrom) <= now
    const toOk = !l.validTo || new Date(l.validTo) >= now
    return fromOk && toOk
  })
  if (active.length > 0) {
    return active.sort((a, b) => new Date(b.validFrom || 0).getTime() - new Date(a.validFrom || 0).getTime())[0]
  }
  return lists[0]
}

function normalizeSku(value: string) {
  return value.trim().toLowerCase()
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


function findBasePrice(list: PriceList, sku: string) {
  const item = list.items.find(i => normalizeSku(i.skuId) === normalizeSku(sku))
  return item ? String(item.basePrice) : ''
}

function storeLabel(store: CatalogProductStore, storeNameMap: Map<string, string>) {
  const base = storeNameMap.get(store.storeId) || store.titleOverride || store.storeId
  return store.isVisible ? base : `${base} (غیرفعال)`
}

function formatNumber(value: number) {
  return value.toLocaleString('fa-IR')
}

function currencyLabel(currency: string) {
  const c = (currency || '').toUpperCase()
  if (c === 'IRR') return 'ریال'
  if (c === 'IRT') return 'تومان'
  return c || 'ریال'
}

function parseNumberOrNull(value?: string | null) {
  if (value == null) return null
  const trimmed = String(value).trim()
  if (!trimmed) return null
  const num = Number(trimmed)
  return Number.isFinite(num) ? num : null
}

function computeOverridePreview(base: number | null, type: OverrideType, value: number | null, currency: string) {
  if (value == null) return null
  const c = (currency || '').toUpperCase()
  const round = (n: number) => (c === 'IRR' || c === 'IRT' ? Math.round(n) : Math.round(n * 100) / 100)

  if (type === 'FixedPrice') return round(Math.max(0, value))
  if (base == null) return null

  if (type === 'PercentOff') return round(Math.max(0, base * (1 - value / 100)))
  if (type === 'AmountOff') return round(Math.max(0, base - value))

  return null
}

function updateBasePrice(
  skuKey: string,
  value: string,
  setState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>,
) {
  setState(prev => ({
    ...prev,
    [skuKey]: { ...prev[skuKey], basePrice: value },
  }))
}

function updateSiteOverrideType(
  skuKey: string,
  siteId: string,
  overrideType: OverrideType,
  setState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>,
) {
  setState(prev => ({
    ...prev,
    [skuKey]: {
      ...prev[skuKey],
      siteOverrides: {
        ...prev[skuKey].siteOverrides,
        [siteId]: {
          ...prev[skuKey].siteOverrides[siteId],
          overrideType,
        },
      },
    },
  }))
}

function updateSiteOverrideValue(
  skuKey: string,
  siteId: string,
  value: string,
  setState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>,
) {
  setState(prev => ({
    ...prev,
    [skuKey]: {
      ...prev[skuKey],
      siteOverrides: {
        ...prev[skuKey].siteOverrides,
        [siteId]: {
          ...prev[skuKey].siteOverrides[siteId],
          value,
        },
      },
    },
  }))
}

function updateDiscountField(
  skuKey: string,
  patch: Partial<DiscountState>,
  setState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>,
) {
  setState(prev => ({
    ...prev,
    [skuKey]: {
      ...prev[skuKey],
      discount: { ...prev[skuKey].discount, ...patch },
    },
  }))
}

async function handleSaveBasePrice({
  skuKey,
  state,
  priceList,
  setSavingBaseSku,
}: {
  skuKey: string
  state: VariantPricingState
  priceList: PriceList | undefined
  setSavingBaseSku: Dispatch<SetStateAction<string | null>>
}) {
  if (!priceList) return
  const value = Number.parseFloat(state.basePrice || '')
  if (!Number.isFinite(value)) {
    swalToastError('قیمت پایه معتبر نیست.')
    return
  }
  setSavingBaseSku(skuKey)
  try {
    await bulkUpdatePrices({
      priceListId: priceList.id,
      currency: priceList.currency,
      items: [{ skuId: state.skuId, basePrice: value }],
    })
    swalToastSuccess('قیمت پایه ذخیره شد.')
  } catch (err) {
    swalToastError(getApiErrorMessage(err, 'خطا در ذخیره قیمت پایه.'))
  } finally {
    setSavingBaseSku(null)
  }
}

function applyGlobalToSites(
  skuKey: string,
  state: VariantPricingState,
  setState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>,
) {
  const value = Number.parseFloat(state.basePrice || '')
  if (!Number.isFinite(value)) {
    swalToastError('ابتدا قیمت پایه را وارد کنید.')
    return
  }
  setState(prev => {
    const next = { ...prev }
    const current = next[skuKey]
    if (!current) return prev
    const updated: Record<string, SiteOverrideState> = {}
    for (const [siteId, override] of Object.entries(current.siteOverrides)) {
      updated[siteId] = { ...override, overrideType: 'FixedPrice', value: String(value) }
    }
    next[skuKey] = { ...current, siteOverrides: updated }
    return next
  })
  swalToastSuccess('قیمت پایه برای همه سایت ها اعمال شد.')
}
async function handleSaveSiteOverrides({
  skuKey,
  state,
  productStores,
  currency,
  setSavingSitesSku,
  setVariantsState,
}: {
  skuKey: string
  state: VariantPricingState
  productStores: CatalogProductStore[]
  currency: string
  setSavingSitesSku: Dispatch<SetStateAction<string | null>>
  setVariantsState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>
}) {
  if (productStores.length === 0) return

  setSavingSitesSku(skuKey)
  try {
    for (const store of productStores) {
      const override = state.siteOverrides[store.storeId]
      if (!override) continue
      const value = Number.parseFloat(override.value || '')
      if (!Number.isFinite(value)) {
        if (!override.value) continue
        throw new Error('مقدار قیمت سایت معتبر نیست.')
      }

      const payload = {
        overrideType: override.overrideType,
        value,
        currency,
        validFrom: null,
        validTo: null,
        priority: 0,
        stackingGroup: 'Product',
      }

      if (override.id) {
        await updateOverride(override.id, payload)
      } else {
        const res = await createOverride({
          scopeType: 'Site',
          scopeId: store.storeId,
          skuId: state.skuId,
          ...payload,
        })
        setVariantsState(prev => ({
          ...prev,
          [skuKey]: {
            ...prev[skuKey],
            siteOverrides: {
              ...prev[skuKey].siteOverrides,
              [store.storeId]: { ...override, id: res.id },
            },
          },
        }))
      }
    }
    swalToastSuccess('قیمت سایت ها ذخیره شد.')
  } catch (err: any) {
    swalToastError(err?.message || getApiErrorMessage(err, 'خطا در ذخیره قیمت سایت ها.'))
  } finally {
    setSavingSitesSku(null)
  }
}

async function handleSaveDiscount({
  skuKey,
  state,
  setSavingDiscountSku,
  setVariantsState,
}: {
  skuKey: string
  state: VariantPricingState
  setSavingDiscountSku: Dispatch<SetStateAction<string | null>>
  setVariantsState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>
}) {
  if (!state.discount.name.trim()) {
    swalToastError('نام کمپین را وارد کنید.')
    return
  }
  if (!state.discount.stackingGroup.trim()) {
    swalToastError('گروه استکینگ را وارد کنید.')
    return
  }

  const benefit = buildBenefit(state.discount.benefit)
  if (!benefit) return

  const guardrails = buildGuardrails(state.discount.maxDiscountPercent, state.discount.maxDiscountAmount)
  const payload = {
    name: state.discount.name.trim(),
    isActive: state.discount.isActive,
    validFrom: state.discount.validFrom || null,
    validTo: state.discount.validTo || null,
    priority: Number.parseInt(state.discount.priority || '0', 10) || 0,
    stackingGroup: state.discount.stackingGroup.trim(),
    stackingMode: state.discount.stackingMode,
    combinableWithOtherPromotions: state.discount.combinableWithOtherPromotions,
    combinableWithCoupons: state.discount.combinableWithCoupons,
    exclusiveGroup: state.discount.exclusiveGroup.trim() || null,
    guardrails,
    eligibility: { kind: 'product', skuIds: [state.skuId] },
    benefit,
  }

  setSavingDiscountSku(skuKey)
  try {
    if (state.campaignId) {
      await updateCampaign(state.campaignId, payload)
      swalToastSuccess('تخفیف ویرایش شد.')
    } else {
      const res = await createCampaign(payload)
      setVariantsState(prev => ({
        ...prev,
        [skuKey]: { ...prev[skuKey], campaignId: res.id, campaignCount: 1 },
      }))
      swalToastSuccess('تخفیف ثبت شد.')
    }
  } catch (err) {
    swalToastError(getApiErrorMessage(err, 'خطا در ثبت تخفیف.'))
  } finally {
    setSavingDiscountSku(null)
  }
}

async function handleDeleteDiscount({
  skuKey,
  state,
  setVariantsState,
}: {
  skuKey: string
  state: VariantPricingState
  setVariantsState: Dispatch<SetStateAction<Record<string, VariantPricingState>>>
}) {
  if (!state.campaignId) return
  const ok = await swalConfirm({
    title: 'حذف تخفیف',
    text: 'آیا از حذف این کمپین مطمئن هستید؟',
    icon: 'warning',
    confirmText: 'بله',
    cancelText: 'خیر',
  })
  if (!ok) return

  try {
    await deleteCampaign(state.campaignId)
    setVariantsState(prev => ({
      ...prev,
      [skuKey]: {
        ...prev[skuKey],
        campaignId: undefined,
        campaignCount: 0,
        discount: makeDiscountState('IRR', 'محصول', prev[skuKey].variantLabel),
      },
    }))
    swalToastSuccess('تخفیف حذف شد.')
  } catch (err) {
    swalToastError(getApiErrorMessage(err, 'خطا در حذف تخفیف.'))
  }
}

function makeDiscountState(currency: string, productName: string, variantLabel: string): DiscountState {
  return {
    name: `تخفیف ${productName} - ${variantLabel}`,
    isActive: true,
    validFrom: '',
    validTo: '',
    priority: '0',
    stackingGroup: 'Product',
    stackingMode: 'BestOfEachGroup',
    combinableWithOtherPromotions: true,
    combinableWithCoupons: true,
    exclusiveGroup: '',
    maxDiscountPercent: '',
    maxDiscountAmount: '',
    benefit: makeDefaultBenefitWithCurrency(currency),
  }
}

function makeDefaultBenefitWithCurrency(currency: string) {
  const def = makeDefaultBenefit()
  if (def.kind === 'amountOff' || def.kind === 'fixedPrice' || def.kind === 'cashbackAmount' || def.kind === 'bundleFixedPrice') {
    return { ...def, currency }
  }
  return def
}

function buildGuardrails(percent: string, amount: string): Guardrails | null {
  const p = percent ? Number.parseFloat(percent) : NaN
  const a = amount ? Number.parseFloat(amount) : NaN
  if (!Number.isFinite(p) && !Number.isFinite(a)) return null
  return {
    maxDiscountPercent: Number.isFinite(p) ? p : null,
    maxDiscountAmount: Number.isFinite(a) ? a : null,
  }
}

function mapCampaignToDiscountState(campaign: PromotionCampaign): DiscountState {
  return {
    name: campaign.name,
    isActive: campaign.isActive,
    validFrom: campaign.validFrom ? toLocalDateTime(campaign.validFrom) : '',
    validTo: campaign.validTo ? toLocalDateTime(campaign.validTo) : '',
    priority: String(campaign.priority ?? 0),
    stackingGroup: campaign.stackingGroup ?? 'Product',
    stackingMode: campaign.stackingMode,
    combinableWithOtherPromotions: campaign.combinableWithOtherPromotions,
    combinableWithCoupons: campaign.combinableWithCoupons,
    exclusiveGroup: campaign.exclusiveGroup ?? '',
    maxDiscountPercent: campaign.guardrails?.maxDiscountPercent?.toString() ?? '',
    maxDiscountAmount: campaign.guardrails?.maxDiscountAmount?.toString() ?? '',
    benefit: mapBenefitToState(campaign.benefit),
  }
}

function mapBenefitToState(def: BenefitDefinition): BenefitBuilderState {
  switch (def.kind) {
    case 'percentOff':
      return { kind: 'percentOff', percent: String(def.percent ?? '') }
    case 'amountOff':
      return { kind: 'amountOff', amount: String(def.amount ?? ''), currency: def.currency ?? 'IRR' }
    case 'fixedPrice':
      return { kind: 'fixedPrice', price: String(def.price ?? ''), currency: def.currency ?? 'IRR' }
    case 'cashbackPercent':
      return { kind: 'cashbackPercent', percent: String(def.percent ?? '') }
    case 'cashbackAmount':
      return { kind: 'cashbackAmount', amount: String(def.amount ?? ''), currency: def.currency ?? 'IRR' }
    case 'bundleFixedPrice':
      return {
        kind: 'bundleFixedPrice',
        requiredItems: def.requiredItems?.map(r => `${r.skuId},${r.qtyRequired}`).join('\n') ?? '',
        bundlePrice: String(def.bundlePrice ?? ''),
        currency: def.currency ?? 'IRR',
      }
    case 'buyXGetY':
      return {
        kind: 'buyXGetY',
        buySkuId: def.buySkuId ?? '',
        buyQty: String(def.buyQty ?? ''),
        getSkuId: def.getSkuId ?? '',
        getQty: String(def.getQty ?? ''),
      }
    default:
      return makeDefaultBenefit()
  }
}

function buildCampaignIndex(list: PromotionCampaign[]) {
  const single = new Map<string, PromotionCampaign>()
  const counts = new Map<string, number>()

  for (const campaign of list) {
    if (campaign.eligibility.kind !== 'product') continue
    const skuIds = campaign.eligibility.skuIds || []
    const normalized = skuIds.map(normalizeSku)
    normalized.forEach(sku => counts.set(sku, (counts.get(sku) ?? 0) + 1))

    if (normalized.length === 1) {
      const key = normalized[0]
      const current = single.get(key)
      if (!current || (campaign.priority ?? 0) > (current.priority ?? 0)) {
        single.set(key, campaign)
      }
    }
  }

  return { single, counts }
}

function toLocalDateTime(value: string) {
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}





















