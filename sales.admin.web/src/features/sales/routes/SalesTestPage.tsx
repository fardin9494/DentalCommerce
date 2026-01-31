import { useEffect, useRef, useState } from 'react'
import { fetchJson, fetchJsonWithBase } from '@/lib/api/client'
import { CATALOG_API_BASE, INVENTORY_API_BASE } from '@/app/env'
import { useToast } from '@/shared/components/toast/ToastProvider'

type Item = { skuId: string; qty: number; batchId?: string }
type StoreOption = { id: string; name: string; domain?: string | null }
type SkuOption = { sku: string; productName?: string | null; variantValue?: string | null; available?: number }

export function SalesTestPage() {
  const toast = useToast()
  const [siteId, setSiteId] = useState('')
  const [userId, setUserId] = useState('')
  const [scenario, setScenario] = useState<'Success' | 'Fail'>('Success')
  const [items, setItems] = useState<Item[]>([{ skuId: '', qty: 1 }])
  const [loading, setLoading] = useState(false)
  const [result, setResult] = useState<any>(null)
  const [error, setError] = useState<string | null>(null)
  const [lookupId, setLookupId] = useState('')
  const [storeSearch, setStoreSearch] = useState('')
  const [storeOptions, setStoreOptions] = useState<StoreOption[]>([])
  const [storeLoading, setStoreLoading] = useState(false)
  const storeTimer = useRef<number | undefined>(undefined)
  const [skuOptions, setSkuOptions] = useState<Record<number, SkuOption[]>>({})
  const skuTimers = useRef<Record<number, number>>({})

  const updateItem = (idx: number, patch: Partial<Item>) => {
    setItems(prev => prev.map((it, i) => i === idx ? { ...it, ...patch } : it))
  }

  const removeItem = (idx: number) => {
    setItems(prev => prev.filter((_, i) => i !== idx))
  }

  useEffect(() => {
    const q = storeSearch.trim()
    if (storeTimer.current) window.clearTimeout(storeTimer.current)
    if (q.length < 2) {
      setStoreOptions([])
      return
    }
    storeTimer.current = window.setTimeout(async () => {
      setStoreLoading(true)
      try {
        const res = await fetchJsonWithBase<StoreOption[]>(CATALOG_API_BASE, `/stores?search=${encodeURIComponent(q)}`)
        setStoreOptions(res || [])
      } catch {
        setStoreOptions([])
      } finally {
        setStoreLoading(false)
      }
    }, 300)
    return () => {
      if (storeTimer.current) window.clearTimeout(storeTimer.current)
    }
  }, [storeSearch])

  const searchSku = async (idx: number, term: string) => {
    const q = term.trim()
    if (q.length < 2) {
      setSkuOptions(prev => ({ ...prev, [idx]: [] }))
      return
    }
    try {
      const res = await fetchJsonWithBase<any>(INVENTORY_API_BASE, `/stock-items?search=${encodeURIComponent(q)}&page=1&pageSize=20&hasStock=true`)
      const rows: any[] = res?.items || []
      const map = new Map<string, SkuOption>()
      for (const r of rows) {
        const sku = r.sku as string
        const available = typeof r.available === 'number' ? r.available : undefined
        if (available !== undefined && available <= 0) continue
        if (!sku || map.has(sku)) continue
        map.set(sku, {
          sku,
          productName: r.productName,
          variantValue: r.variantValue,
          available,
        })
      }
      setSkuOptions(prev => ({ ...prev, [idx]: Array.from(map.values()) }))
    } catch {
      setSkuOptions(prev => ({ ...prev, [idx]: [] }))
    }
  }

  const handleSkuInput = (idx: number, value: string) => {
    updateItem(idx, { skuId: value })
    const timers = skuTimers.current
    if (timers[idx]) window.clearTimeout(timers[idx])
    timers[idx] = window.setTimeout(() => searchSku(idx, value), 250)
  }

  const selectSku = (idx: number, sku: string) => {
    updateItem(idx, { skuId: sku })
    setSkuOptions(prev => ({ ...prev, [idx]: [] }))
  }

  const selectStore = (s: StoreOption) => {
    setSiteId(s.id)
    const label = s.domain ? `${s.name} (${s.domain})` : s.name
    setStoreSearch(label)
    setStoreOptions([])
  }

  async function handleCheckout() {
    setError(null)
    setResult(null)
    setLoading(true)
    try {
      const body = {
        siteId: siteId.trim(),
        userId: userId.trim() || null,
        couponCode: null,
        paymentScenario: scenario,
        items: items
          .filter(i => i.skuId.trim())
          .map(i => ({
            skuId: i.skuId.trim(),
            qty: Number(i.qty || 0),
            batchId: i.batchId?.trim() || null,
          })),
      }
      const res = await fetchJson<any>('/sales/checkout', { json: body })
      setResult(res)
      toast.success('درخواست انجام شد')
    } catch (err: any) {
      const msg = err?.message || 'خطا در انجام عملیات'
      setError(msg.length > 200 ? `${msg.slice(0, 200)}...` : msg)
      setResult(err?.details ?? err)
      toast.error('درخواست ناموفق بود')
    } finally {
      setLoading(false)
    }
  }

  async function handleLoadOrder() {
    setError(null)
    setResult(null)
    const id = lookupId.trim()
    if (!id) return
    setLoading(true)
    try {
      const res = await fetchJson<any>(`/sales/orders/${id}`)
      setResult(res)
      toast.info('سفارش دریافت شد')
    } catch (err: any) {
      const msg = err?.message || 'خطا در دریافت سفارش'
      setError(msg.length > 200 ? `${msg.slice(0, 200)}...` : msg)
      setResult(err?.details ?? err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      <div className="card p-4">
        <h2 className="text-lg font-semibold mb-4">تست سناریوی خرید و پرداخت</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="relative">
            <label className="label">سایت (Store)</label>
            <input
              className="input"
              value={storeSearch}
              onChange={e => setStoreSearch(e.target.value)}
              placeholder="جستجوی نام یا دامنه"
            />
            {storeLoading && <div className="text-xs text-gray-500 mt-1">در حال جستجو...</div>}
            {storeOptions.length > 0 && (
              <div className="absolute z-20 mt-1 w-full bg-white border rounded shadow max-h-60 overflow-auto">
                {storeOptions.map(s => (
                  <button
                    key={s.id}
                    type="button"
                    className="block w-full text-right px-3 py-2 text-sm hover:bg-gray-50"
                    onClick={() => selectStore(s)}
                  >
                    <div className="font-medium">{s.name}</div>
                    <div className="text-xs text-gray-500">{s.domain || s.id}</div>
                  </button>
                ))}
              </div>
            )}
            <div className="text-xs text-gray-500 mt-1">SiteId: {siteId || '—'}</div>
          </div>
          <div>
            <label className="label">UserId (اختیاری)</label>
            <input className="input" value={userId} onChange={e => setUserId(e.target.value)} placeholder="GUID کاربر" />
          </div>
          <div>
            <label className="label">سناریو پرداخت</label>
            <select className="input" value={scenario} onChange={e => setScenario(e.target.value as any)}>
              <option value="Success">موفق</option>
              <option value="Fail">ناموفق</option>
            </select>
          </div>
        </div>

        <div className="mt-6">
          <div className="flex items-center justify-between mb-2">
            <h3 className="font-semibold">آیتم‌ها</h3>
            <button className="btn-secondary" onClick={() => setItems(prev => [...prev, { skuId: '', qty: 1 }])}>افزودن آیتم</button>
          </div>
          <div className="space-y-3">
            {items.map((it, idx) => (
              <div key={idx} className="grid grid-cols-1 md:grid-cols-4 gap-3">
                <div className="relative">
                  <input
                    className="input"
                    placeholder="SKU (جستجو کنید)"
                    value={it.skuId}
                    onChange={e => handleSkuInput(idx, e.target.value)}
                  />
                  {skuOptions[idx]?.length ? (
                    <div className="absolute z-20 mt-1 w-full bg-white border rounded shadow max-h-60 overflow-auto">
                      {skuOptions[idx].map(opt => (
                        <button
                          key={opt.sku}
                          type="button"
                          className="block w-full text-right px-3 py-2 text-sm hover:bg-gray-50"
                          onClick={() => selectSku(idx, opt.sku)}
                        >
                          <div className="font-medium">{opt.sku}</div>
                          <div className="text-xs text-gray-500">
                            {(opt.productName || '')}
                            {opt.variantValue ? ` - ${opt.variantValue}` : ''}
                            {typeof opt.available === 'number' ? ` | موجودی: ${opt.available}` : ''}
                          </div>
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
                <input className="input" type="number" min={1} placeholder="Qty" value={it.qty} onChange={e => updateItem(idx, { qty: Number(e.target.value) })} />
                <input className="input" placeholder="BatchId (اختیاری)" value={it.batchId || ''} onChange={e => updateItem(idx, { batchId: e.target.value })} />
                <div className="flex items-center gap-2">
                  <button className="btn-red px-3 py-2.5" onClick={() => removeItem(idx)}>حذف</button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="mt-6 flex items-center gap-3">
          <button className="btn" onClick={handleCheckout} disabled={loading}>ارسال سناریو</button>
          {error && <span className="text-sm text-red-600">{error}</span>}
        </div>
      </div>

      <div className="card p-4">
        <h3 className="font-semibold mb-3">دریافت سفارش با شناسه</h3>
        <div className="flex flex-col md:flex-row gap-3">
          <input className="input" placeholder="OrderId" value={lookupId} onChange={e => setLookupId(e.target.value)} />
          <button className="btn-secondary" onClick={handleLoadOrder} disabled={loading}>دریافت</button>
        </div>
      </div>

      <div className="card p-4">
        <h3 className="font-semibold mb-2">خروجی</h3>
        <pre className="text-xs whitespace-pre-wrap bg-gray-50 p-3 rounded border">{result ? JSON.stringify(result, null, 2) : '---'}</pre>
      </div>
    </div>
  )
}
