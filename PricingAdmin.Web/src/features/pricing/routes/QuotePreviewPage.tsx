import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { swalToastError } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { createQuote, getApiErrorMessage } from '../api'
import type { QuoteRequest, QuoteResponse } from '../types'
import { SkuPicker } from '../components/SkuPicker'
import { formatSkuLabel } from '../catalogSkuLabels'
import { listCatalogStores } from '../catalogApi'
import type { CatalogStoreListItem } from '../catalogTypes'

type QuoteItemForm = {
  id: number
  skuId: string
  skuLabel: string
  qty: number
  batchId: string
}

export function QuotePreviewPage() {
  const [siteId, setSiteId] = useState('')
  const [stores, setStores] = useState<CatalogStoreListItem[]>([])
  const [storesLoading, setStoresLoading] = useState(false)
  const [storeSearch, setStoreSearch] = useState('')

  const [userId, setUserId] = useState('')
  const [couponCode, setCouponCode] = useState('')
  const [timestamp, setTimestamp] = useState('')
  const [items, setItems] = useState<QuoteItemForm[]>([
    { id: 1, skuId: '', skuLabel: '', qty: 1, batchId: '' },
  ])
  const [quote, setQuote] = useState<QuoteResponse | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let active = true
    const load = async () => {
      setStoresLoading(true)
      try {
        const data = await listCatalogStores()
        if (!active) return
        setStores(data || [])
      } catch (err) {
        if (!active) return
        swalToastError(getApiErrorMessage(err, 'خطا در دریافت فهرست سایت‌ها از کاتالوگ.'))
      } finally {
        if (active) setStoresLoading(false)
      }
    }
    void load()
    return () => {
      active = false
    }
  }, [])

  const filteredStores = useMemo(() => {
    const q = storeSearch.trim().toLowerCase()
    if (!q) return stores
    return stores.filter(s => {
      const name = (s.name || '').toLowerCase()
      const domain = (s.domain || '').toLowerCase()
      return name.includes(q) || domain.includes(q) || s.id.toLowerCase().includes(q)
    })
  }, [storeSearch, stores])

  const addItem = () => {
    setItems(prev => [...prev, { id: Date.now(), skuId: '', skuLabel: '', qty: 1, batchId: '' }])
  }

  const removeItem = (id: number) => {
    setItems(prev => prev.filter(i => i.id !== id))
  }

  const updateItem = (id: number, patch: Partial<QuoteItemForm>) => {
    setItems(prev => prev.map(i => (i.id === id ? { ...i, ...patch } : i)))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const trimmedSiteId = siteId.trim()
    if (!trimmedSiteId) {
      swalToastError('سایت را انتخاب کنید.')
      return
    }
    const cleanItems = items
      .map(i => ({
        skuId: i.skuId.trim(),
        qty: Number(i.qty),
        batchId: i.batchId.trim() || null,
      }))
      .filter(i => i.skuId && i.qty > 0)

    if (cleanItems.length === 0) {
      swalToastError('حداقل یک آیتم معتبر وارد کنید.')
      return
    }

    const payload: QuoteRequest = {
      siteId: trimmedSiteId,
      userId: userId.trim() || null,
      couponCode: couponCode.trim() || null,
      timestamp: timestamp.trim() || null,
      items: cleanItems,
    }

    setLoading(true)
    try {
      const res = await createQuote(payload)
      setQuote(res)
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در دریافت پیش‌نمایش قیمت.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader title="پیش‌نمایش قیمت">
        یک سبد تست بسازید و خروجی موتور قیمت‌گذاری را ببینید: قیمت نهایی، منابع اعمال‌شده/ردشده و ردیابی تصمیم‌ها.
      </PageHeader>

      <div className="card p-4">
        <form className="space-y-4" onSubmit={handleSubmit}>
          <div className="rounded-md border border-slate-200 bg-slate-50 p-3 text-xs text-gray-700">
            مرحله 1: سایت را انتخاب کنید. مرحله 2: اقلام سبد را اضافه کنید. مرحله 3: (اختیاری) کوپن/کاربر/زمان را وارد کنید. مرحله 4: محاسبه.
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="border rounded-md p-3 space-y-3">
              <div className="font-semibold text-sm">تنظیمات محاسبه</div>

              <div>
                <label className="label">سایت</label>
                <input
                  className="input mb-2"
                  value={storeSearch}
                  onChange={e => setStoreSearch(e.target.value)}
                  placeholder="جستجو در سایت‌ها (نام/دامنه)..."
                />
                <select
                  className="input"
                  value={siteId}
                  onChange={e => setSiteId(e.target.value)}
                  disabled={storesLoading || stores.length === 0}
                >
                  <option value="">
                    {storesLoading ? 'در حال دریافت سایت‌ها...' : stores.length === 0 ? 'سایتی یافت نشد' : 'انتخاب سایت'}
                  </option>
                  {filteredStores.map(s => (
                    <option key={s.id} value={s.id}>
                      {s.name}{s.domain ? ` (${s.domain})` : ''} — {s.id}
                    </option>
                  ))}
                </select>
                <details className="mt-2 text-xs text-gray-600">
                  <summary className="cursor-pointer">ورود دستی شناسه سایت (GUID)</summary>
                  <input className="input mt-2" value={siteId} onChange={e => setSiteId(e.target.value)} placeholder="SiteId (GUID)" />
                </details>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className="label">شناسه کاربر (اختیاری)</label>
                  <input className="input" value={userId} onChange={e => setUserId(e.target.value)} placeholder="UserId (GUID)" />
                </div>
                <div>
                  <label className="label">کد کوپن (اختیاری)</label>
                  <input className="input" value={couponCode} onChange={e => setCouponCode(e.target.value)} placeholder="مثال: OFF10" />
                </div>
              </div>

              <div>
                <label className="label">زمان محاسبه (اختیاری)</label>
                <JalaliDateTimePicker value={timestamp} onChange={setTimestamp} clearable />
                <div className="mt-1 text-xs text-gray-600">اگر خالی باشد، زمان فعلی استفاده می‌شود.</div>
              </div>
            </div>

            <div className="border rounded-md p-3 space-y-3">
              <div className="flex items-center justify-between">
                <div className="font-semibold text-sm">سبد تست</div>
                <div className="flex items-center gap-2">
                  <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={addItem}>افزودن آیتم</button>
                  <button
                    type="button"
                    className="btn-secondary px-3 py-1.5 rounded"
                    onClick={() => {
                      setItems([{ id: 1, skuId: '', skuLabel: '', qty: 1, batchId: '' }])
                      setQuote(null)
                    }}
                  >
                    پاک کردن سبد
                  </button>
                </div>
              </div>

              <div className="overflow-x-auto">
                <table className="min-w-full text-sm text-center">
                  <thead>
                    <tr className="bg-gray-100">
                      <th className="p-2 text-right">محصول/واریانت</th>
                      <th className="p-2">تعداد</th>
                      <th className="p-2">شناسه بچ</th>
                      <th className="p-2">عملیات</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map(item => (
                      <tr key={item.id} className="border-b last:border-0">
                        <td className="p-2 text-right min-w-[320px]">
                          <SkuPicker
                            label="انتخاب محصول/واریانت"
                            value={item.skuId}
                            onChange={(next) => updateItem(item.id, { skuId: next.skuId, skuLabel: next.label })}
                            showLabel={false}
                          />
                        </td>
                        <td className="p-2 w-32">
                          <input
                            className="input text-center"
                            type="number"
                            inputMode="numeric"
                            min={1}
                            value={item.qty || ''}
                            onChange={e => {
                              const val = e.target.value.trim()
                              const num = val === '' ? 1 : Number(val)
                              updateItem(item.id, { qty: num > 0 ? num : 1 })
                            }}
                          />
                        </td>
                        <td className="p-2 w-64">
                          <input
                            className="input text-center"
                            value={item.batchId}
                            onChange={e => updateItem(item.id, { batchId: e.target.value })}
                            placeholder="اختیاری (GUID)"
                          />
                        </td>
                        <td className="p-2 w-28">
                          {items.length > 1 ? (
                            <button type="button" className="btn-red px-3 py-1.5 rounded" onClick={() => removeItem(item.id)}>
                              حذف
                            </button>
                          ) : (
                            <span className="text-xs text-gray-400">—</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button type="submit" className="btn" disabled={loading}>
              {loading ? 'در حال محاسبه...' : 'محاسبه قیمت'}
            </button>
            {quote ? (
              <button type="button" className="btn-secondary px-3 py-2 rounded" onClick={() => setQuote(null)}>
                پاک کردن نتیجه
              </button>
            ) : null}
          </div>
        </form>
      </div>

      {loading && <Spinner />}

      {quote && (
        <div className="space-y-4">
          <div className="card p-4">
            <h3 className="font-semibold mb-3">خلاصه قیمت</h3>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-3 text-sm">
              <div>جمع پایه: <span className="font-semibold">{formatNumber(quote.subtotal)}</span></div>
              <div>تخفیف: <span className="font-semibold">{formatNumber(quote.discountTotal)}</span></div>
              <div>قیمت نهایی: <span className="font-semibold">{formatNumber(quote.finalTotal)}</span></div>
              <div>کش‌بک: <span className="font-semibold">{formatNumber(quote.cashbackTotal)}</span></div>
            </div>
          </div>

          <div className="card p-4">
            <h3 className="font-semibold mb-3">اقلام</h3>
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm text-center">
                <thead>
                  <tr className="bg-gray-100">
                    <th className="p-2">محصول/واریانت</th>
                    <th className="p-2">تعداد</th>
                    <th className="p-2">قیمت پایه</th>
                    <th className="p-2">قیمت نهایی</th>
                    <th className="p-2">هدیه</th>
                    <th className="p-2">تعدیلات</th>
                  </tr>
                </thead>
                <tbody>
                  {quote.lines.map((line, idx) => (
                    <tr key={`${line.skuId}-${idx}`} className="border-b last:border-0">
                      <td className="p-2">
                        {formatSkuLabel(line.skuId) ? (
                          <div>
                            <div className="font-semibold">{formatSkuLabel(line.skuId)}</div>
                            <div className="text-xs text-gray-600">{line.skuId}</div>
                          </div>
                        ) : line.skuId}
                      </td>
                      <td className="p-2">{line.quantity}</td>
                      <td className="p-2">{formatNumber(line.baseUnitPrice)}</td>
                      <td className="p-2">{formatNumber(line.finalUnitPrice)}</td>
                      <td className="p-2">{line.isGift ? 'بله' : 'خیر'}</td>
                      <td className="p-2">
                        {line.adjustments?.length ? (
                          <ul className="text-xs text-gray-700 space-y-1">
                            {line.adjustments.map((a, i) => (
                              <li key={i}>
                                {a.description} ({formatNumber(a.amount)})
                              </li>
                            ))}
                          </ul>
                        ) : '-'}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="card p-4">
              <h3 className="font-semibold mb-3">منابع اعمال‌شده</h3>
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm text-center">
                  <thead>
                    <tr className="bg-gray-100">
                      <th className="p-2">نوع</th>
                      <th className="p-2">نام</th>
                      <th className="p-2">اولویت</th>
                      <th className="p-2">گروه</th>
                    </tr>
                  </thead>
                  <tbody>
                    {quote.appliedSources.map((s, i) => (
                      <tr key={i} className="border-b last:border-0">
                        <td className="p-2">{s.sourceType}</td>
                        <td className="p-2">{s.name}</td>
                        <td className="p-2">{s.priority ?? '-'}</td>
                        <td className="p-2">{s.stackingGroup ?? '-'}</td>
                      </tr>
                    ))}
                    {quote.appliedSources.length === 0 && (
                      <tr className="border-b last:border-0">
                        <td colSpan={4} className="p-3 text-sm text-gray-600">منبعی اعمال نشده است.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="card p-4">
              <h3 className="font-semibold mb-3">منابع ردشده</h3>
              <div className="overflow-x-auto">
                <table className="min-w-full text-sm text-center">
                  <thead>
                    <tr className="bg-gray-100">
                      <th className="p-2">نوع</th>
                      <th className="p-2">نام</th>
                      <th className="p-2">دلیل</th>
                    </tr>
                  </thead>
                  <tbody>
                    {quote.rejectedSources.map((s, i) => (
                      <tr key={i} className="border-b last:border-0">
                        <td className="p-2">{s.sourceType}</td>
                        <td className="p-2">{s.name}</td>
                        <td className="p-2">{s.reason ?? '-'}</td>
                      </tr>
                    ))}
                    {quote.rejectedSources.length === 0 && (
                      <tr className="border-b last:border-0">
                        <td colSpan={3} className="p-3 text-sm text-gray-600">منبعی رد نشده است.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>

          <div className="card p-4">
              <h3 className="font-semibold mb-3">ردیابی</h3>
            <ul className="text-sm text-gray-700 space-y-1">
              {quote.trace.map((t, i) => (
                <li key={i}>
                  <span className="font-semibold">{t.stage}:</span> {t.message}
                </li>
              ))}
              {quote.trace.length === 0 && <li className="text-gray-600">ردیابی ثبت نشده است.</li>}
            </ul>
          </div>
        </div>
      )}
    </div>
  )
}

function formatNumber(value: number) {
  return value.toLocaleString('fa-IR')
}
