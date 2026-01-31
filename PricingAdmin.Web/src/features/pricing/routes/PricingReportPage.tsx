import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { swalToastError } from '../../../shared/utils/swal'
import { searchCatalogProducts, getCatalogProduct, listCatalogStores } from '../catalogApi'
import type { CatalogProductDetail, CatalogProductListItem, CatalogProductVariant, CatalogProductStore, CatalogStoreListItem } from '../catalogTypes'
import { getSkuLabel, setSkuLabel } from '../catalogSkuLabels'
import type { PriceList, PriceListItem, PriceOverride, PromotionCampaign, OverrideType, QuoteResponse } from '../types'
import { RejectedSourcesPanel } from '../components/RejectedSourcesPanel'
import { useCampaigns, useOverrides, usePriceList, usePriceLists } from '../queries'
import { createQuote, getApiErrorMessage } from '../api'

type ExpandedKey = string
type DiscountTag = { label: string; kind: 'override' | 'campaign' }

export function PricingReportPage() {
  const [search, setSearch] = useState('')
  const [searching, setSearching] = useState(false)
  const [searchResults, setSearchResults] = useState<CatalogProductListItem[]>([])
  const [searchError, setSearchError] = useState<string | null>(null)
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [highlightedIndex, setHighlightedIndex] = useState(-1)
  const searchBoxRef = useRef<HTMLDivElement>(null)

  const [allQuery, setAllQuery] = useState('')
  const [onlyDiscounted, setOnlyDiscounted] = useState(false)
  const [catalogStores, setCatalogStores] = useState<CatalogStoreListItem[]>([])
  const [catalogStoresLoading, setCatalogStoresLoading] = useState(false)
  const [finalSiteId, setFinalSiteId] = useState('')
  const [finalUserId, setFinalUserId] = useState('')
  const [finalQty, setFinalQty] = useState('1')
  const [finalResults, setFinalResults] = useState<Record<string, { quote?: QuoteResponse; error?: string }>>({})
  const [finalLoading, setFinalLoading] = useState<Record<string, boolean>>({})
  const [expandedAll, setExpandedAll] = useState<Record<string, boolean>>({})
  const [labelVersion, setLabelVersion] = useState(0)
  const [catalogIssues, setCatalogIssues] = useState<Record<string, string>>({})

  const [selectedProducts, setSelectedProducts] = useState<CatalogProductDetail[]>([])
  const [loadingProductId, setLoadingProductId] = useState<string | null>(null)
  const [expanded, setExpanded] = useState<Record<ExpandedKey, boolean>>({})

  const { data: priceLists } = usePriceLists()
  const activePriceList = useMemo(() => pickActivePriceList(priceLists), [priceLists])
  const { data: priceList } = usePriceList(activePriceList?.id)
  const currency = priceList?.currency || 'IRR'

  const { data: overrides, isLoading: loadingOverrides } = useOverrides({})
  const { data: campaigns, isLoading: loadingCampaigns } = useCampaigns()

  const priceIndex = useMemo(() => buildPriceIndex(priceList), [priceList])
  const overrideIndex = useMemo(() => buildOverrideIndex(overrides ?? []), [overrides])
  const campaignIndex = useMemo(() => buildCampaignIndex(campaigns ?? []), [campaigns])

  const runSearch = useCallback(async (term: string) => {
    const normalized = term.trim()
    if (!normalized) {
      setSearchResults([])
      setSearchError(null)
      return
    }
    if (normalized.length < 2) {
      setSearchResults([])
      setSearchError('برای جستجو حداقل دو حرف وارد کنید.')
      return
    }
    setSearching(true)
    setSearchError(null)
    try {
      const res = await searchCatalogProducts({ search: normalized, page: 1, pageSize: 20 })
      setSearchResults(res.items || [])
      setSearchError((res.items || []).length === 0 ? 'موردی یافت نشد.' : null)
    } catch (err) {
      const msg = getApiErrorMessage(err, 'خطا در جستجوی کاتالوگ.')
      setSearchResults([])
      setSearchError(msg)
      swalToastError(msg)
    } finally {
      setSearching(false)
    }
  }, [])

  useEffect(() => {
    let active = true
    const loadStores = async () => {
      setCatalogStoresLoading(true)
      try {
        const stores = await listCatalogStores()
        if (!active) return
        setCatalogStores(stores || [])
      } catch (err) {
        if (!active) return
        swalToastError(getApiErrorMessage(err, 'خطا در دریافت لیست سایت‌ها از کاتالوگ.'))
      } finally {
        if (active) setCatalogStoresLoading(false)
      }
    }
    void loadStores()
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    const h = window.setTimeout(() => {
      void runSearch(search)
    }, 350)
    return () => window.clearTimeout(h)
  }, [search, runSearch])

  useEffect(() => {
    const onDocClick = (e: MouseEvent) => {
      const target = e.target as Node
      if (!searchBoxRef.current) return
      if (searchBoxRef.current.contains(target)) return
      setIsSearchOpen(false)
    }
    document.addEventListener('mousedown', onDocClick)
    return () => document.removeEventListener('mousedown', onDocClick)
  }, [])

  const handleSelectProduct = async (item: CatalogProductListItem) => {
    if (selectedProducts.some(p => p.id === item.id)) {
      setIsSearchOpen(false)
      return
    }
    setLoadingProductId(item.id)
    try {
      const detail = await getCatalogProduct(item.id)
      setSelectedProducts(prev => [detail, ...prev])
      seedSkuLabels(detail)
      setIsSearchOpen(false)
      setSearch(item.name || '')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در دریافت جزئیات محصول.'))
    } finally {
      setLoadingProductId(null)
    }
  }

  const toggleExpanded = (key: ExpandedKey) => {
    setExpanded(prev => ({ ...prev, [key]: !prev[key] }))
  }

  const toggleExpandedAll = (key: ExpandedKey) => {
    setExpandedAll(prev => ({ ...prev, [key]: !prev[key] }))
  }

  const loadFinalPriceForSku = useCallback(async (skuId: string) => {
    const sku = skuId.trim()
    if (!sku) return
    if (!finalSiteId.trim()) {
      swalToastError('SiteId را وارد کنید.')
      return
    }
    const qty = Number.parseInt(finalQty || '1', 10)
    if (!Number.isFinite(qty) || qty <= 0) {
      swalToastError('تعداد معتبر نیست.')
      return
    }
    const key = normalizeSku(sku)
    const issue = catalogIssues[key]
    if (issue) {
      swalToastError(issue)
      return
    }
    setFinalLoading(prev => ({ ...prev, [key]: true }))
    try {
      const quote = await createQuote({
        siteId: finalSiteId.trim(),
        userId: finalUserId.trim() || null,
        couponCode: null,
        timestamp: null,
        items: [{ skuId: sku, qty, batchId: null }],
      })
      setFinalResults(prev => ({ ...prev, [key]: { quote } }))
    } catch (err) {
      const msg = getApiErrorMessage(err, 'خطا در محاسبه قیمت نهایی.')
      if (isCatalogMissingError(msg)) {
        const friendly = `این SKU در کاتالوگ یافت نشد: ${sku}`
        setCatalogIssues(prev => ({ ...prev, [key]: friendly }))
        setFinalResults(prev => ({ ...prev, [key]: { error: friendly } }))
      } else {
        setFinalResults(prev => ({ ...prev, [key]: { error: msg } }))
      }
    } finally {
      setFinalLoading(prev => ({ ...prev, [key]: false }))
    }
  }, [finalSiteId, finalUserId, finalQty, catalogIssues])

  const now = new Date()
  const pricedItems = useMemo(() => {
    if (!priceList) return []
    const q = allQuery.trim().toLowerCase()
    return (priceList.items || [])
      .map(item => {
        const sku = item.skuId
        const skuKey = normalizeSku(sku)
        const { productName, variantName } = splitSkuLabel(sku)
        const siteOverrides = (overrideIndex.get(skuKey)?.site ?? []).filter(o => isActiveAt(o, now))
        const userOverrides = (overrideIndex.get(skuKey)?.user ?? []).filter(o => isActiveAt(o, now))
        const relatedCampaigns = (campaignIndex.get(skuKey) ?? []).filter(c => isActiveAtCampaign(c, now))
        const discountTags = buildDiscountTags(siteOverrides, userOverrides, relatedCampaigns)
        return { item, sku, skuKey, productName, variantName, siteOverrides, userOverrides, relatedCampaigns, discountTags }
      })
      .filter(row => {
        if (onlyDiscounted && row.siteOverrides.length === 0 && row.userOverrides.length === 0 && row.relatedCampaigns.length === 0) return false
        if (!q) return true
        const label = `${row.productName} ${row.variantName}`.toLowerCase()
        return row.sku.toLowerCase().includes(q) || label.includes(q)
      })
  }, [priceList, allQuery, onlyDiscounted, overrideIndex, campaignIndex, now, labelVersion])

  const autoResolveQueueRef = useRef<string[]>([])
  const autoResolvingRef = useRef(new Set<string>())
  const autoResolvedRef = useRef(new Set<string>())
  const autoResolveCountRef = useRef(0)

  const resolveSkuLabel = useCallback(async (skuId: string) => {
    const sku = skuId.trim()
    if (!sku) return null
    const res = await searchCatalogProducts({ search: sku, page: 1, pageSize: 8 })
    const candidates = res.items || []
    for (const item of candidates) {
      const detail = await getCatalogProduct(item.id)
      const match = findSkuLabel(detail, sku)
      if (match) return match
    }
    return null
  }, [])

  useEffect(() => {
    const missing = pricedItems
      .filter(row => row.productName === 'نام محصول نامشخص')
      .map(row => row.sku)
      .filter(sku => {
        const key = normalizeSku(sku)
        return !autoResolvedRef.current.has(key) && !autoResolvingRef.current.has(key)
      })

    if (missing.length === 0) return
    if (autoResolveCountRef.current >= 20) return

    autoResolveQueueRef.current = missing.slice(0, 6)
    let cancelled = false

    const run = async () => {
      for (const sku of autoResolveQueueRef.current) {
        if (cancelled) return
        const key = normalizeSku(sku)
        if (autoResolvingRef.current.has(key) || autoResolvedRef.current.has(key)) continue
        autoResolvingRef.current.add(key)
        try {
          const label = await resolveSkuLabel(sku)
          if (label) {
            setSkuLabel(sku, label)
            setLabelVersion(v => v + 1)
          }
        } catch {
          // ignore
        } finally {
          autoResolvedRef.current.add(key)
          autoResolvingRef.current.delete(key)
          autoResolveCountRef.current += 1
        }
        await new Promise(resolve => setTimeout(resolve, 200))
      }
    }

    void run()
    return () => {
      cancelled = true
    }
  }, [pricedItems, resolveSkuLabel])

  return (
    <div className="space-y-4">
      <PageHeader title="گزارش قیمت‌ها">
        در این صفحه می‌توانید قیمت پایه، قیمت‌های ویژه (سایت/کاربر) و کمپین‌های مرتبط با هر محصول/واریانت را یکجا ببینید.
      </PageHeader>

      <div className="card p-4 space-y-4">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
          <div>
            <div className="font-semibold">لیست محصولات قیمت‌دار</div>
            <div className="text-xs text-gray-600">
              {priceList ? `${formatNumber(priceList.items.length)} آیتم در ${priceList.name}` : 'لیست قیمت فعالی یافت نشد.'}
            </div>
          </div>
          <div className="text-xs text-gray-600">
            {loadingOverrides ? 'در حال دریافت قیمت‌های ویژه...' : `قیمت‌های ویژه: ${formatNumber((overrides ?? []).length)}`}
            {' | '}
            {loadingCampaigns ? 'در حال دریافت کمپین‌ها...' : `کمپین‌ها: ${formatNumber((campaigns ?? []).length)}`}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-3">
          <div className="lg:col-span-2">
            <label className="label">جستجو (نام محصول/کد)</label>
            <input
              className="input"
              value={allQuery}
              onChange={e => setAllQuery(e.target.value)}
              placeholder="نام یا SKU را وارد کنید..."
            />
          </div>
          <div className="flex items-end">
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" checked={onlyDiscounted} onChange={e => setOnlyDiscounted(e.target.checked)} />
              فقط آیتم‌های دارای تخفیف/کمپین
            </label>
          </div>
        </div>

        <div className="border rounded-md p-3 bg-gray-50 space-y-3">
          <div className="font-semibold text-sm">محاسبه قیمت نهایی (اختیاری)</div>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
            <div>
              <label className="label">سایت</label>
              <select
                className="input"
                value={finalSiteId}
                onChange={e => setFinalSiteId(e.target.value)}
                disabled={catalogStoresLoading}
              >
                <option value="">{catalogStoresLoading ? 'در حال دریافت سایت‌ها...' : 'انتخاب سایت'}</option>
                {catalogStores.map(s => (
                  <option key={s.id} value={s.id}>
                    {s.name}{s.domain ? ` (${s.domain})` : ''}
                  </option>
                ))}
              </select>
              {!catalogStoresLoading && catalogStores.length === 0 && (
                <div className="text-xs text-gray-500 mt-1">سایتی از کاتالوگ یافت نشد.</div>
              )}
            </div>
            <div>
              <label className="label">UserId (اختیاری)</label>
              <input className="input" value={finalUserId} onChange={e => setFinalUserId(e.target.value)} placeholder="Guid کاربر" />
            </div>
            <div>
              <label className="label">تعداد</label>
              <input className="input" type="number" min={1} value={finalQty} onChange={e => setFinalQty(e.target.value)} />
            </div>
          </div>
          <div className="text-xs text-gray-600">
            برای هر ردیف می‌توانید روی «محاسبه» کلیک کنید تا قیمت نهایی همان SKU را از موتور قیمت‌گذاری دریافت کنید.
          </div>
        </div>

        {pricedItems.length === 0 ? (
          <div className="text-sm text-gray-600">آیتمی برای نمایش وجود ندارد.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">محصول</th>
                  <th className="p-2">واریانت</th>
                  <th className="p-2">SKU</th>
                  <th className="p-2">قیمت پایه</th>
                  <th className="p-2">ویژه سایت</th>
                  <th className="p-2">ویژه کاربر</th>
                  <th className="p-2">کمپین‌ها</th>
                  <th className="p-2">نوع تخفیف</th>
                  <th className="p-2">قیمت نهایی</th>
                  <th className="p-2">جزئیات</th>
                </tr>
              </thead>
              <tbody>
                {pricedItems.map(row => {
                  const final = finalResults[row.skuKey]?.quote
                  const error = finalResults[row.skuKey]?.error
                  const line = final?.lines.find(l => normalizeSku(l.skuId) === row.skuKey && !l.isGift)
                  const catalogIssue = catalogIssues[row.skuKey]
                  return (
                    <>
                      <tr key={row.skuKey} className="border-b last:border-0">
                        <td className="p-2 text-right">
                          <div className="font-medium">{row.productName}</div>
                        </td>
                        <td className="p-2 text-right text-xs text-gray-600">{row.variantName}</td>
                        <td className="p-2 font-mono text-xs">{row.sku}</td>
                        <td className="p-2">{formatNumber(row.item.basePrice)} {currencyLabel(currency)}</td>
                        <td className="p-2">{row.siteOverrides.length === 0 ? '—' : formatNumber(row.siteOverrides.length)}</td>
                        <td className="p-2">{row.userOverrides.length === 0 ? '—' : formatNumber(row.userOverrides.length)}</td>
                        <td className="p-2">{row.relatedCampaigns.length === 0 ? '—' : formatNumber(row.relatedCampaigns.length)}</td>
                        <td className="p-2">
                          {row.discountTags.length === 0 ? (
                            <span className="text-xs text-gray-500">بدون تخفیف</span>
                          ) : (
                            <div className="flex flex-wrap items-center justify-center gap-1">
                              {row.discountTags.slice(0, 4).map(tag => (
                                <span key={tag.label} className={`badge ${tag.kind === 'campaign' ? 'badge-green' : 'badge-gray'}`}>
                                  {tag.label}
                                </span>
                              ))}
                              {row.discountTags.length > 4 && (
                                <span className="text-xs text-gray-500">+{formatNumber(row.discountTags.length - 4)}</span>
                              )}
                            </div>
                          )}
                        </td>
                        <td className="p-2">
                          {catalogIssue ? (
                            <span className="text-red-600">ناموجود در کاتالوگ</span>
                          ) : error ? (
                            <span className="text-red-600">خطا</span>
                          ) : line ? (
                            <span>{formatNumber(line.finalUnitPrice)} {currencyLabel(final?.currency || currency)}</span>
                          ) : (
                            '—'
                          )}
                        </td>
                        <td className="p-2">
                          <div className="flex items-center justify-center gap-2">
                            <button
                              type="button"
                              className="btn-secondary px-3 py-2 rounded"
                              onClick={() => loadFinalPriceForSku(row.sku)}
                              disabled={finalLoading[row.skuKey] || !!catalogIssue}
                            >
                              {finalLoading[row.skuKey] ? 'در حال محاسبه...' : 'محاسبه'}
                            </button>
                            <button
                              type="button"
                              className="btn-secondary px-3 py-2 rounded"
                              onClick={() => toggleExpandedAll(row.skuKey)}
                            >
                              {expandedAll[row.skuKey] ? 'بستن' : 'نمایش'}
                            </button>
                          </div>
                        </td>
                      </tr>
                      {expandedAll[row.skuKey] && (
                        <tr className="border-b last:border-0 bg-gray-50">
                          <td colSpan={10} className="p-3 text-right">
                            <div className="grid grid-cols-1 lg:grid-cols-4 gap-3 text-xs">
                              <div className="border rounded-md p-2 bg-white">
                                <div className="font-semibold mb-1">قیمت و پلکان</div>
                                <div>پایه: {formatNumber(row.item.basePrice)} {currencyLabel(currency)}</div>
                                {row.item.tierPrices?.length ? (
                                  <div className="mt-2 space-y-1">
                                    {row.item.tierPrices.map((t, i) => (
                                      <div key={i}>از {t.minQty}: {formatNumber(t.unitPrice)} {currencyLabel(currency)}</div>
                                    ))}
                                  </div>
                                ) : (
                                  <div className="text-gray-600 mt-1">قیمت پلکانی ندارد.</div>
                                )}
                              </div>
                              <div className="border rounded-md p-2 bg-white">
                                <div className="font-semibold mb-1">قیمت‌های ویژه</div>
                                <div className="space-y-1">
                                  <div>سایت: {row.siteOverrides.length === 0 ? '—' : formatNumber(row.siteOverrides.length)}</div>
                                  <div>کاربر: {row.userOverrides.length === 0 ? '—' : formatNumber(row.userOverrides.length)}</div>
                                </div>
                              </div>
                              <div className="border rounded-md p-2 bg-white">
                                <div className="font-semibold mb-1">کمپین‌های مرتبط</div>
                                {row.relatedCampaigns.length === 0 ? (
                                  <div className="text-gray-600">موردی نیست.</div>
                                ) : (
                                  <ul className="space-y-1">
                                    {row.relatedCampaigns.slice(0, 6).map(c => (
                                      <li key={c.id}>{c.name} | {benefitShortLabel(c.benefit, currency)}</li>
                                    ))}
                                    {row.relatedCampaigns.length > 6 && <li>...</li>}
                                  </ul>
                                )}
                              </div>
                              <div className="border rounded-md p-2 bg-white">
                                <div className="font-semibold mb-1">انواع تخفیف</div>
                                {row.discountTags.length === 0 ? (
                                  <div className="text-gray-600">بدون تخفیف</div>
                                ) : (
                                  <div className="flex flex-wrap gap-1">
                                    {row.discountTags.map(tag => (
                                      <span key={tag.label} className={`badge ${tag.kind === 'campaign' ? 'badge-green' : 'badge-gray'}`}>
                                        {tag.label}
                                      </span>
                                    ))}
                                  </div>
                                )}
                              </div>
                            </div>

                            {final && (
                              <div className="mt-3 grid grid-cols-1 lg:grid-cols-3 gap-3 text-xs">
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">جمع‌بندی Quote</div>
                                  <div>Subtotal: {formatNumber(final.subtotal)}</div>
                                  <div>DiscountTotal: {formatNumber(final.discountTotal)}</div>
                                  <div>FinalTotal: {formatNumber(final.finalTotal)}</div>
                                  <div>CashbackTotal: {formatNumber(final.cashbackTotal)}</div>
                                </div>
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">تعدیلات</div>
                                  {line?.adjustments?.length ? (
                                    <ul className="space-y-1">
                                      {line.adjustments.map((a, idx) => (
                                        <li key={idx}>{a.description}: {formatNumber(a.amount)}</li>
                                      ))}
                                    </ul>
                                  ) : (
                                    <div className="text-gray-600">موردی نیست.</div>
                                  )}
                                </div>
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">منابع اعمال‌شده</div>
                                  {final.appliedSources.length === 0 ? (
                                    <div className="text-gray-600">موردی نیست.</div>
                                  ) : (
                                    <ul className="space-y-1">
                                      {final.appliedSources.map(s => (
                                        <li key={`${s.sourceType}-${s.sourceId}`}>{s.name} ({s.sourceType})</li>
                                      ))}
                                    </ul>
                                  )}
                                </div>
                              </div>
                            )}

                            {error && (
                              <div className="mt-2 text-xs text-red-600">{error}</div>
                            )}
                          </td>
                        </tr>
                      )}
                    </>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="card p-4 space-y-3">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
          <div>
            <div className="font-semibold">لیست قیمت فعال</div>
            <div className="text-xs text-gray-600">
              {activePriceList ? `${activePriceList.name} (${currencyLabel(currency)})` : 'لیست قیمت فعالی یافت نشد.'}
            </div>
          </div>
          <div className="text-xs text-gray-600">
            {loadingOverrides ? 'در حال دریافت قیمت‌های ویژه...' : `قیمت‌های ویژه: ${formatNumber((overrides ?? []).length)}`}
            {' | '}
            {loadingCampaigns ? 'در حال دریافت کمپین‌ها...' : `کمپین‌ها: ${formatNumber((campaigns ?? []).length)}`}
          </div>
        </div>

        <div ref={searchBoxRef} className="relative">
          <label className="label">جستجوی محصول (کاتالوگ)</label>
          <input
            className="input"
            value={search}
            onChange={e => {
              setSearch(e.target.value)
              setIsSearchOpen(true)
              setHighlightedIndex(-1)
            }}
            onFocus={() => setIsSearchOpen(true)}
            onKeyDown={(e) => {
              if (!isSearchOpen) return
              if (e.key === 'Escape') {
                setIsSearchOpen(false)
                return
              }
              if (e.key === 'ArrowDown') {
                e.preventDefault()
                setHighlightedIndex(prev => Math.min(prev + 1, searchResults.length - 1))
                return
              }
              if (e.key === 'ArrowUp') {
                e.preventDefault()
                setHighlightedIndex(prev => Math.max(prev - 1, 0))
                return
              }
              if (e.key === 'Enter') {
                if (highlightedIndex < 0 || highlightedIndex >= searchResults.length) return
                void handleSelectProduct(searchResults[highlightedIndex])
              }
            }}
            placeholder="حداقل دو حرف از نام محصول..."
          />

          {isSearchOpen && (searching || searchError || searchResults.length > 0) && (
            <div className="absolute z-20 mt-2 w-full bg-white border rounded-md shadow overflow-hidden">
              {searching && (
                <div className="p-3 text-sm text-gray-600 flex items-center gap-2">
                  <Spinner />
                  در حال جستجو...
                </div>
              )}
              {!searching && searchError && (
                <div className="p-3 text-sm text-red-600">{searchError}</div>
              )}
              {!searching && !searchError && (
                <ul className="max-h-72 overflow-auto">
                  {searchResults.map((r, idx) => {
                    const active = idx === highlightedIndex
                    return (
                      <li key={r.id}>
                        <button
                          type="button"
                          className={`w-full text-right px-3 py-2 hover:bg-gray-50 ${active ? 'bg-gray-100' : ''}`}
                          onMouseEnter={() => setHighlightedIndex(idx)}
                          onClick={() => void handleSelectProduct(r)}
                          disabled={loadingProductId === r.id}
                        >
                          <div className="flex items-center justify-between gap-2">
                            <div className="font-medium">{r.name}</div>
                            <div className="text-xs text-gray-500">{r.brandName || ''}</div>
                          </div>
                          <div className="text-xs text-gray-600 mt-1">
                            {r.code ? `کد: ${r.code}` : ''}
                            {selectedProducts.some(p => p.id === r.id) ? ' (اضافه شده)' : ''}
                          </div>
                        </button>
                      </li>
                    )
                  })}
                </ul>
              )}
            </div>
          )}
        </div>
      </div>

      {selectedProducts.length === 0 ? (
        <div className="card p-4 text-sm text-gray-600">برای شروع، یک یا چند محصول را جستجو و به گزارش اضافه کنید.</div>
      ) : (
        <div className="space-y-4">
          {selectedProducts.map(p => {
            const variants = getPricingVariants(p)
            const stores = p.stores || []
            return (
              <div key={p.id} className="card p-4 space-y-3">
                <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-2">
                  <div>
                    <div className="font-semibold">{p.name}</div>
                    <div className="text-xs text-gray-600">
                      {p.brandName ? `${p.brandName} | ` : ''}
                      {p.status || ''}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="btn-red px-3 py-2 rounded"
                    onClick={() => setSelectedProducts(prev => prev.filter(x => x.id !== p.id))}
                  >
                    حذف از گزارش
                  </button>
                </div>

                {stores.length > 0 && (
                  <div className="flex flex-wrap gap-2 text-xs">
                    {stores.map(s => (
                      <span key={s.storeId} className={`badge ${s.isVisible ? 'badge-gray' : 'badge-gray opacity-60'}`}>
                        {storeLabel(s)}
                      </span>
                    ))}
                  </div>
                )}

                <div className="overflow-x-auto">
                  <table className="min-w-full text-sm text-center">
                    <thead>
                      <tr className="bg-gray-100">
                        <th className="p-2">محصول/واریانت</th>
                        <th className="p-2">SKU</th>
                        <th className="p-2">قیمت پایه</th>
                        <th className="p-2">ویژه سایت</th>
                        <th className="p-2">ویژه کاربر</th>
                        <th className="p-2">کمپین‌های مرتبط</th>
                        <th className="p-2">جزئیات</th>
                      </tr>
                    </thead>
                    <tbody>
                      {variants.map(v => {
                        const sku = v.sku
                        const key = `${p.id}:${normalizeSku(sku)}`
                          const label = getSkuLabel(sku) || `${p.name}${v.value ? ` - ${v.value}` : ''}`
                        const item = priceIndex.get(normalizeSku(sku)) || null
                        const basePrice = item?.basePrice ?? null
                        const siteOverrides = (overrideIndex.get(normalizeSku(sku))?.site ?? []).filter(o => isActiveAt(o, now))
                        const userOverrides = (overrideIndex.get(normalizeSku(sku))?.user ?? []).filter(o => isActiveAt(o, now))
                        const relatedCampaigns = (campaignIndex.get(normalizeSku(sku)) ?? []).filter(c => isActiveAtCampaign(c, now))

                        return (
                          <>
                            <tr key={key} className="border-b last:border-0">
                              <td className="p-2 text-right">
                                <div className="font-medium">{label}</div>
                                {!v.isActive && <div className="text-xs text-gray-500">(غیرفعال در کاتالوگ)</div>}
                              </td>
                              <td className="p-2 font-mono text-xs">{sku}</td>
                              <td className="p-2">
                                {basePrice == null ? (
                                  <span className="text-amber-700">ثبت نشده</span>
                                ) : (
                                  <span>{formatNumber(basePrice)} {currencyLabel(currency)}</span>
                                )}
                              </td>
                              <td className="p-2">{siteOverrides.length === 0 ? '—' : formatNumber(siteOverrides.length)}</td>
                              <td className="p-2">{userOverrides.length === 0 ? '—' : formatNumber(userOverrides.length)}</td>
                              <td className="p-2">{relatedCampaigns.length === 0 ? '—' : formatNumber(relatedCampaigns.length)}</td>
                              <td className="p-2">
                                <button type="button" className="btn-secondary px-3 py-2 rounded" onClick={() => toggleExpanded(key)}>
                                  {expanded[key] ? 'بستن' : 'نمایش'}
                                </button>
                              </td>
                            </tr>

                            {expanded[key] && (
                              <tr className="border-b last:border-0 bg-gray-50">
                                <td colSpan={7} className="p-3 text-right">
                                    <VariantDetails
                                      sku={sku}
                                      currency={currency}
                                      item={item}
                                      stores={stores}
                                      basePrice={basePrice}
                                      siteOverrides={siteOverrides}
                                      userOverrides={userOverrides}
                                      relatedCampaigns={relatedCampaigns}
                                      catalogIssue={catalogIssues[normalizeSku(sku)]}
                                      onCatalogIssue={(skuId, message) => setCatalogIssues(prev => ({ ...prev, [normalizeSku(skuId)]: message }))}
                                    />
                                </td>
                              </tr>
                            )}
                          </>
                        )
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}

function VariantDetails(props: {
  sku: string
  currency: string
  item: PriceListItem | null
  stores: CatalogProductStore[]
  basePrice: number | null
  siteOverrides: PriceOverride[]
  userOverrides: PriceOverride[]
  relatedCampaigns: PromotionCampaign[]
  catalogIssue?: string
  onCatalogIssue?: (skuId: string, message: string) => void
}) {
  const storeIndex = useMemo(() => new Map(props.stores.map(s => [s.storeId, s])), [props.stores])
  const base = props.basePrice
  const [quoteUserId, setQuoteUserId] = useState('')
  const [quoteQty, setQuoteQty] = useState('1')
  const [loadingQuotes, setLoadingQuotes] = useState(false)
  const [siteQuotes, setSiteQuotes] = useState<Record<string, { quote?: QuoteResponse; error?: string }>>({})
  const [expandedSites, setExpandedSites] = useState<Record<string, boolean>>({})

  const toggleSiteDetails = (storeId: string) => {
    setExpandedSites(prev => ({ ...prev, [storeId]: !prev[storeId] }))
  }

  const loadFinalPrices = async () => {
    if (props.stores.length === 0) {
      swalToastError('سایتی برای این محصول ثبت نشده است.')
      return
    }
    if (props.catalogIssue) {
      swalToastError(props.catalogIssue)
      return
    }
    const qty = Number.parseInt(quoteQty || '1', 10)
    if (!Number.isFinite(qty) || qty <= 0) {
      swalToastError('تعداد معتبر نیست.')
      return
    }
    setLoadingQuotes(true)
    setSiteQuotes({})
    try {
      const results: Record<string, { quote?: QuoteResponse; error?: string }> = {}
        for (const store of props.stores) {
          try {
            const quote = await createQuote({
              siteId: store.storeId,
              userId: quoteUserId.trim() || null,
              couponCode: null,
              timestamp: null,
              items: [{ skuId: props.sku, qty, batchId: null }],
            })
            results[store.storeId] = { quote }
          } catch (err) {
            const msg = getApiErrorMessage(err, 'خطا در محاسبه قیمت نهایی.')
            if (isCatalogMissingError(msg)) {
              const friendly = `این SKU در کاتالوگ یافت نشد: ${props.sku}`
              props.onCatalogIssue?.(props.sku, friendly)
              results[store.storeId] = { error: friendly }
            } else {
              results[store.storeId] = { error: msg }
            }
          }
        }
      setSiteQuotes(results)
    } finally {
      setLoadingQuotes(false)
    }
  }

  return (
    <div className="space-y-3">
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3">
        <div className="border rounded-md p-3 bg-white">
          <div className="font-semibold text-sm mb-2">قیمت پایه</div>
          <div className="text-sm">
            {base == null ? (
              <span className="text-amber-700">برای این SKU قیمت پایه ثبت نشده است.</span>
            ) : (
              <span>{formatNumber(base)} {currencyLabel(props.currency)}</span>
            )}
          </div>
          {props.item?.tierPrices?.length ? (
            <div className="mt-2 text-xs text-gray-700">
              <div className="font-semibold mb-1">قیمت پلکانی:</div>
              <ul className="space-y-1">
                {props.item.tierPrices.map((t, i) => (
                  <li key={i}>از {t.minQty} عدد: {formatNumber(t.unitPrice)} {currencyLabel(props.currency)}</li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>

        <div className="border rounded-md p-3 bg-white">
          <div className="font-semibold text-sm mb-2">قیمت‌های ویژه سایت</div>
          {props.siteOverrides.length === 0 ? (
            <div className="text-sm text-gray-600">موردی وجود ندارد.</div>
          ) : (
            <div className="space-y-2">
              {props.siteOverrides
                .slice()
                .sort((a, b) => b.priority - a.priority)
                .map(o => {
                  const store = storeIndex.get(o.scopeId)
                  const label = store ? storeLabel(store) : o.scopeId
                  const preview = computeOverridePreview(base, o.overrideType, o.value, props.currency)
                  return (
                    <div key={o.id} className="border rounded-md p-2">
                      <div className="flex items-center justify-between gap-2">
                        <div className="font-medium text-sm">{label}</div>
                        <div className="text-xs text-gray-600">اولویت: {o.priority}</div>
                      </div>
                      <div className="text-xs text-gray-700 mt-1">
                        {overrideLabel(o.overrideType, o.value, props.currency)}
                        {o.validFrom || o.validTo ? ` | بازه: ${o.validFrom || '—'} تا ${o.validTo || '—'}` : ''}
                      </div>
                      <div className="text-xs text-gray-600 mt-1">
                        {preview == null
                          ? 'پیش‌نمایش قیمت نهایی نیاز به قیمت پایه دارد.'
                          : `پیش‌نمایش قیمت نهایی: ${formatNumber(preview)} ${currencyLabel(props.currency)}`}
                      </div>
                    </div>
                  )
                })}
            </div>
          )}
        </div>

        <div className="border rounded-md p-3 bg-white">
          <div className="font-semibold text-sm mb-2">قیمت‌های ویژه کاربر</div>
          {props.userOverrides.length === 0 ? (
            <div className="text-sm text-gray-600">موردی وجود ندارد.</div>
          ) : (
            <div className="space-y-2">
              {props.userOverrides
                .slice()
                .sort((a, b) => b.priority - a.priority)
                .map(o => {
                  const preview = computeOverridePreview(base, o.overrideType, o.value, props.currency)
                  return (
                    <div key={o.id} className="border rounded-md p-2">
                      <div className="flex items-center justify-between gap-2">
                        <div className="font-medium text-sm">کاربر: {o.scopeId}</div>
                        <div className="text-xs text-gray-600">اولویت: {o.priority}</div>
                      </div>
                      <div className="text-xs text-gray-700 mt-1">
                        {overrideLabel(o.overrideType, o.value, props.currency)}
                        {o.validFrom || o.validTo ? ` | بازه: ${o.validFrom || '—'} تا ${o.validTo || '—'}` : ''}
                      </div>
                      <div className="text-xs text-gray-600 mt-1">
                        {preview == null
                          ? 'پیش‌نمایش قیمت نهایی نیاز به قیمت پایه دارد.'
                          : `پیش‌نمایش قیمت نهایی: ${formatNumber(preview)} ${currencyLabel(props.currency)}`}
                      </div>
                    </div>
                  )
                })}
            </div>
          )}
        </div>
      </div>

      <div className="border rounded-md p-3 bg-white">
        <div className="font-semibold text-sm mb-2">کمپین‌های مرتبط (نام‌بردن مستقیم از SKU)</div>
        {props.relatedCampaigns.length === 0 ? (
          <div className="text-sm text-gray-600">موردی یافت نشد.</div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-2">
            {props.relatedCampaigns
              .slice()
              .sort((a, b) => b.priority - a.priority)
              .map(c => (
                <div key={c.id} className="border rounded-md p-2">
                  <div className="flex items-center justify-between gap-2">
                    <div className="font-medium text-sm">{c.name}</div>
                    <div className="text-xs text-gray-600">اولویت: {c.priority}</div>
                  </div>
                  <div className="text-xs text-gray-700 mt-1">
                    گروه: {c.stackingGroup || '—'} | مدل: {c.stackingMode} | انحصار: {c.exclusiveGroup || '—'}
                  </div>
                  <div className="text-xs text-gray-700 mt-1">
                    مزیت: {benefitShortLabel(c.benefit, props.currency)}
                  </div>
                  <div className="text-xs text-gray-600 mt-1">
                    {c.isActive ? 'فعال' : 'غیرفعال'}
                    {c.validFrom || c.validTo ? ` | بازه: ${c.validFrom || '—'} تا ${c.validTo || '—'}` : ''}
                  </div>
                </div>
              ))}
          </div>
        )}
        <div className="text-xs text-gray-500 mt-2">
          نکته: کمپین‌های دسته/برند/تگ در این گزارش فقط زمانی نمایش داده می‌شوند که SKU به‌صورت مستقیم در شرط/مزیت ذکر شده باشد (برای تطبیق کامل نیاز به متادیتای دسته/برند از کاتالوگ است).
        </div>
      </div>

      <div className="border rounded-md p-3 bg-white space-y-3">
        <div className="flex flex-col lg:flex-row lg:items-end lg:justify-between gap-3">
          <div>
            <div className="font-semibold text-sm">قیمت نهایی بر اساس سایت (موتور قیمت‌گذاری)</div>
            <div className="text-xs text-gray-600">این بخش قیمت نهایی را از /pricing/quote می‌گیرد و تغییرات اعمال‌شده را نمایش می‌دهد.</div>
          </div>
          <div className="flex flex-col md:flex-row gap-2 md:items-end">
            <div>
              <label className="label">UserId (اختیاری)</label>
              <input className="input" value={quoteUserId} onChange={e => setQuoteUserId(e.target.value)} placeholder="Guid کاربر" />
            </div>
            <div className="w-28">
              <label className="label">تعداد</label>
              <input className="input" type="number" min={1} value={quoteQty} onChange={e => setQuoteQty(e.target.value)} />
            </div>
            <button className="btn" onClick={loadFinalPrices} disabled={loadingQuotes}>
              {loadingQuotes ? 'در حال محاسبه...' : 'محاسبه قیمت نهایی'}
            </button>
          </div>
        </div>

        {props.stores.length === 0 ? (
          <div className="text-sm text-gray-600">سایتی برای این محصول ثبت نشده است.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">سایت</th>
                  <th className="p-2">قیمت نهایی (واحد)</th>
                  <th className="p-2">جمع کل</th>
                  <th className="p-2">میزان تخفیف</th>
                  <th className="p-2">کش‌بک</th>
                  <th className="p-2">جزئیات</th>
                </tr>
              </thead>
              <tbody>
                {props.stores.map(store => {
                  const label = storeLabel(store)
                  const result = siteQuotes[store.storeId]
                  const quote = result?.quote
                  const line = quote?.lines.find(l => normalizeSku(l.skuId) === normalizeSku(props.sku) && !l.isGift)
                  const error = result?.error
                  return (
                    <>
                      <tr key={store.storeId} className="border-b last:border-0">
                        <td className="p-2">{label}</td>
                        <td className="p-2">
                          {error
                            ? <span className="text-red-600">خطا</span>
                            : line
                              ? `${formatNumber(line.finalUnitPrice)} ${currencyLabel(quote?.currency || props.currency)}`
                              : quote
                                ? '—'
                                : '—'}
                        </td>
                        <td className="p-2">{quote ? `${formatNumber(quote.finalTotal)} ${currencyLabel(quote.currency)}` : '—'}</td>
                        <td className="p-2">{quote ? formatNumber(quote.discountTotal) : '—'}</td>
                        <td className="p-2">{quote ? formatNumber(quote.cashbackTotal) : '—'}</td>
                        <td className="p-2">
                          <button type="button" className="btn-secondary px-3 py-2 rounded" onClick={() => toggleSiteDetails(store.storeId)} disabled={!quote && !error}>
                            {expandedSites[store.storeId] ? 'بستن' : 'نمایش'}
                          </button>
                        </td>
                      </tr>
                      {expandedSites[store.storeId] && (
                        <tr className="border-b last:border-0 bg-gray-50">
                          <td colSpan={6} className="p-3 text-right">
                            {error ? (
                              <div className="text-sm text-red-600">{error}</div>
                            ) : quote ? (
                              <div className="grid grid-cols-1 lg:grid-cols-3 gap-3 text-xs">
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">تنظیمات قیمت این SKU</div>
                                  {line ? (
                                    <div className="space-y-1">
                                      <div>BaseUnitPrice: {formatNumber(line.baseUnitPrice)}</div>
                                      <div>FinalUnitPrice: {formatNumber(line.finalUnitPrice)}</div>
                                      <div>تعداد: {formatNumber(line.quantity)}</div>
                                    </div>
                                  ) : (
                                    <div className="text-gray-600">خطی برای این SKU یافت نشد.</div>
                                  )}
                                  {line?.adjustments?.length ? (
                                    <div className="mt-2 space-y-1">
                                      <div className="font-semibold">تعدیلات:</div>
                                      {line.adjustments.map((a, idx) => (
                                        <div key={idx}>
                                          {a.description} ({a.sourceType}): {formatNumber(a.amount)}
                                        </div>
                                      ))}
                                    </div>
                                  ) : null}
                                </div>
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">منابع اعمال شده</div>
                                  {quote.appliedSources.length === 0 ? (
                                    <div className="text-gray-600">موردی نیست.</div>
                                  ) : (
                                    <ul className="space-y-1">
                                      {quote.appliedSources.map(s => (
                                        <li key={`${s.sourceType}-${s.sourceId}`}>{s.name} ({s.sourceType})</li>
                                      ))}
                                    </ul>
                                  )}
                                </div>
                                <div className="border rounded-md p-2 bg-white">
                                  <div className="font-semibold mb-1">منابع رد شده / Trace</div>
                                  <RejectedSourcesPanel rejectedSources={quote.rejectedSources} />
                                  {quote.trace.length > 0 && (
                                    <div className="mt-2 space-y-1 text-gray-600">
                                      {quote.trace.slice(0, 5).map((t, idx) => (
                                        <div key={idx}>{t.stage}: {t.message}</div>
                                      ))}
                                      {quote.trace.length > 5 && <div>...</div>}
                                    </div>
                                  )}
                                </div>
                              </div>
                            ) : (
                              <div className="text-sm text-gray-600">برای مشاهده جزئیات، ابتدا محاسبه را انجام دهید.</div>
                            )}
                          </td>
                        </tr>
                      )}
                    </>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}

function seedSkuLabels(detail: CatalogProductDetail) {
  const variants = (detail.variants || []).filter(v => v.sku)
  if (variants.length > 0) {
    variants.forEach(v => {
      const label = `${detail.name} - ${v.value || v.sku}`
      setSkuLabel(v.sku, label)
    })
    return
  }
  if (detail.code) {
    setSkuLabel(detail.code, detail.name)
  }
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

function splitSkuLabel(skuId: string) {
  const sku = skuId.trim()
  const stored = getSkuLabel(skuId)
  const label = stored && stored.trim().length > 0 ? stored.trim() : ''
  if (!label || label.toLowerCase() === sku.toLowerCase()) {
    return { productName: 'نام محصول نامشخص', variantName: '—' }
  }
  const parts = label.split(' - ').map(p => p.trim()).filter(Boolean)
  if (parts.length >= 2) {
    return {
      productName: parts[0],
      variantName: parts.slice(1).join(' - '),
    }
  }
  return { productName: label, variantName: 'محصول اصلی' }
}

function findSkuLabel(detail: CatalogProductDetail, skuId: string) {
  const sku = normalizeSku(skuId)
  if (!sku) return null
  const variant = (detail.variants || []).find(v => normalizeSku(v.sku) === sku)
  if (variant) return `${detail.name} - ${variant.value || variant.sku}`
  if (detail.code && normalizeSku(detail.code) === sku) return detail.name
  return null
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

function storeLabel(store: CatalogProductStore) {
  const base = store.titleOverride || store.slug || store.storeId
  return store.isVisible ? base : `${base} (غیرفعال)`
}

function currencyLabel(currency: string) {
  const c = (currency || '').toUpperCase()
  if (c === 'IRR') return 'ریال'
  if (c === 'IRT') return 'تومان'
  return c || 'ریال'
}

function formatNumber(value: number) {
  return value.toLocaleString('fa-IR')
}

function buildPriceIndex(priceList?: PriceList | null) {
  const map = new Map<string, PriceListItem>()
  if (!priceList) return map
  for (const item of priceList.items || []) {
    map.set(normalizeSku(item.skuId), item)
  }
  return map
}

function buildOverrideIndex(overrides: PriceOverride[]) {
  const map = new Map<string, { site: PriceOverride[]; user: PriceOverride[] }>()
  for (const o of overrides) {
    const key = normalizeSku(o.skuId)
    const bucket = map.get(key) || { site: [], user: [] }
    if (o.scopeType === 'Site') bucket.site.push(o)
    else bucket.user.push(o)
    map.set(key, bucket)
  }
  return map
}

function buildCampaignIndex(campaigns: PromotionCampaign[]) {
  const map = new Map<string, PromotionCampaign[]>()
  for (const c of campaigns) {
    const skus = extractSkuMentions(c)
    for (const sku of skus) {
      const key = normalizeSku(sku)
      const arr = map.get(key) || []
      arr.push(c)
      map.set(key, arr)
    }
  }
  return map
}

function buildDiscountTags(
  siteOverrides: PriceOverride[],
  userOverrides: PriceOverride[],
  campaigns: PromotionCampaign[],
): DiscountTag[] {
  const tags: DiscountTag[] = []
  const seen = new Set<string>()
  const add = (label: string, kind: DiscountTag['kind']) => {
    if (seen.has(label)) return
    seen.add(label)
    tags.push({ label, kind })
  }

  for (const o of siteOverrides) {
    add(`${overrideTypeLabelShort(o.overrideType)} (سایت)`, 'override')
  }
  for (const o of userOverrides) {
    add(`${overrideTypeLabelShort(o.overrideType)} (کاربر)`, 'override')
  }
  for (const c of campaigns) {
    add(benefitKindLabelShort(c.benefit), 'campaign')
  }

  return tags
}

function isActiveAt(o: PriceOverride, at: Date) {
  const fromOk = !o.validFrom || new Date(o.validFrom) <= at
  const toOk = !o.validTo || new Date(o.validTo) >= at
  return fromOk && toOk
}

function isActiveAtCampaign(c: PromotionCampaign, at: Date) {
  if (!c.isActive) return false
  const fromOk = !c.validFrom || new Date(c.validFrom) <= at
  const toOk = !c.validTo || new Date(c.validTo) >= at
  return fromOk && toOk
}

function computeOverridePreview(base: number | null, type: OverrideType, value: number, currency: string) {
  const c = (currency || '').toUpperCase()
  const round = (n: number) => (c === 'IRR' || c === 'IRT' ? Math.round(n) : Math.round(n * 100) / 100)

  if (type === 'FixedPrice') return round(Math.max(0, value))
  if (base == null) return null
  if (type === 'PercentOff') return round(Math.max(0, base * (1 - value / 100)))
  if (type === 'AmountOff') return round(Math.max(0, base - value))
  return null
}

function overrideLabel(type: OverrideType, value: number, currency: string) {
  if (type === 'PercentOff') return `درصدی کاهش: ${formatNumber(value)}%`
  if (type === 'AmountOff') return `ریالی کاهش: ${formatNumber(value)} ${currencyLabel(currency)}`
  return `قیمت ثابت: ${formatNumber(value)} ${currencyLabel(currency)}`
}

function overrideTypeLabelShort(type: OverrideType) {
  if (type === 'PercentOff') return 'ویژه درصدی'
  if (type === 'AmountOff') return 'ویژه ریالی'
  return 'ویژه قیمت ثابت'
}

function benefitKindLabelShort(benefit: any) {
  if (!benefit || typeof benefit !== 'object') return 'کمپین'
  switch (benefit.kind) {
    case 'percentOff':
      return 'کمپین درصدی'
    case 'amountOff':
      return 'کمپین ریالی'
    case 'fixedPrice':
      return 'کمپین قیمت ثابت'
    case 'cashbackPercent':
      return 'کش‌بک درصدی'
    case 'cashbackAmount':
      return 'کش‌بک ریالی'
    case 'bundleFixedPrice':
      return 'باندل قیمت ثابت'
    case 'buyXGetY':
      return 'بخر X بگیر Y'
    default:
      return benefit.kind || 'کمپین'
  }
}

function benefitShortLabel(benefit: any, currency: string) {
  if (!benefit || typeof benefit !== 'object') return '—'
  switch (benefit.kind) {
    case 'percentOff':
      return `درصدی: ${formatNumber(benefit.percent)}%`
    case 'amountOff':
      return `ریالی: ${formatNumber(benefit.amount)} ${currencyLabel(benefit.currency || currency)}`
    case 'fixedPrice':
      return `قیمت ثابت: ${formatNumber(benefit.price)} ${currencyLabel(benefit.currency || currency)}`
    case 'cashbackPercent':
      return `کش‌بک درصدی: ${formatNumber(benefit.percent)}%`
    case 'cashbackAmount':
      return `کش‌بک ریالی: ${formatNumber(benefit.amount)} ${currencyLabel(benefit.currency || currency)}`
    case 'buyXGetY':
      return `بخر ${benefit.buyQty} تا بگیر ${benefit.getQty}`
    case 'bundleFixedPrice':
      return `باندل قیمت ثابت: ${formatNumber(benefit.bundlePrice)} ${currencyLabel(benefit.currency || currency)}`
    default:
      return benefit.kind || '—'
  }
}

function isCatalogMissingError(message: string) {
  const text = String(message || '').toLowerCase()
  return text.includes('catalog metadata not resolved')
    || text.includes('metadata not resolved')
}

function extractSkuMentions(c: PromotionCampaign) {
  const out = new Set<string>()

  // Eligibility direct product list
  collectEligibilitySkus(c.eligibility, out)

  // Benefit references
  const b: any = c.benefit as any
  if (b?.kind === 'buyXGetY') {
    if (b.buySkuId) out.add(String(b.buySkuId))
    if (b.getSkuId) out.add(String(b.getSkuId))
  }
  if (b?.kind === 'bundleFixedPrice') {
    const req = Array.isArray(b.requiredItems) ? b.requiredItems : []
    req.forEach((r: any) => r?.skuId && out.add(String(r.skuId)))
    const legacy = Array.isArray(b.skuIds) ? b.skuIds : []
    legacy.forEach((s: any) => out.add(String(s)))
  }

  return Array.from(out).filter(Boolean)
}

function collectEligibilitySkus(el: any, out: Set<string>) {
  if (!el || typeof el !== 'object') return
  if (el.kind === 'product' && Array.isArray(el.skuIds)) {
    el.skuIds.forEach((s: any) => out.add(String(s)))
    return
  }
  if ((el.kind === 'allOf' || el.kind === 'anyOf') && Array.isArray(el.conditions)) {
    el.conditions.forEach((c: any) => collectEligibilitySkus(c, out))
  }
  if (el.kind === 'bundle' && Array.isArray(el.requirements)) {
    el.requirements.forEach((r: any) => r?.skuId && out.add(String(r.skuId)))
  }
}
