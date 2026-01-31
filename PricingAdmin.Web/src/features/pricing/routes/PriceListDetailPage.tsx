import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { formatJalaliDate } from '../../../shared/utils/date'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { getApiErrorMessage } from '../api'
import { useBulkUpdatePrices, useDeletePriceList, usePriceList, useUpdatePriceList } from '../queries'
import type { PriceList, TierPrice } from '../types'
import { SkuPicker } from '../components/SkuPicker'
import { formatSkuLabel } from '../catalogSkuLabels'

type ListFormState = {
  name: string
  currency: string
  validFrom: string
  validTo: string
  isActive: boolean
}

type ItemFormState = {
  skuId: string
  basePrice: string
  tierText: string
}

type ParsedItem = {
  skuId: string
  basePrice: number
  tierPrices: TierPrice[]
}

export function PriceListDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { data, isLoading } = usePriceList(id)
  const update = useUpdatePriceList(id || '')
  const del = useDeletePriceList()
  const bulk = useBulkUpdatePrices(id)

  const [listForm, setListForm] = useState<ListFormState>({
    name: '',
    currency: 'IRR',
    validFrom: '',
    validTo: '',
    isActive: true,
  })
  const [itemForm, setItemForm] = useState<ItemFormState>({ skuId: '', basePrice: '', tierText: '' })
  const [pasteText, setPasteText] = useState('')
  const [preview, setPreview] = useState<ParsedItem[]>([])

  useEffect(() => {
    if (!data) return
    setListForm({
      name: data.name,
      currency: data.currency,
      validFrom: data.validFrom ? toLocalDateTime(data.validFrom) : '',
      validTo: data.validTo ? toLocalDateTime(data.validTo) : '',
      isActive: data.isActive,
    })
  }, [data])

  const items = useMemo(() => data?.items ?? [], [data])

  const handleListSave = async (e: FormEvent) => {
    e.preventDefault()
    if (!id) return
    try {
      await update.mutateAsync({
        name: listForm.name.trim(),
        currency: listForm.currency.trim(),
        validFrom: listForm.validFrom || null,
        validTo: listForm.validTo || null,
        isActive: listForm.isActive,
      })
      swalToastSuccess('لیست قیمت به‌روزرسانی شد.')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در به‌روزرسانی لیست قیمت.'))
    }
  }

  const handleDelete = async () => {
    if (!id) return
    const ok = await swalConfirm({
      title: 'حذف لیست قیمت',
      text: 'این عملیات غیرقابل بازگشت است. ادامه می‌دهید؟',
      icon: 'warning',
      confirmText: 'بله',
      cancelText: 'خیر',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      swalToastSuccess('لیست قیمت حذف شد.')
      navigate('/pricing/pricelists')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در حذف لیست قیمت.'))
    }
  }

  const handleItemUpsert = async (e: FormEvent) => {
    e.preventDefault()
    if (!data) return
    const sku = itemForm.skuId.trim()
    const basePrice = parseNumber(itemForm.basePrice)
    if (!sku || basePrice === null) {
      swalToastError('کد کالا و قیمت پایه الزامی است.')
      return
    }
    try {
      const tierPrices = parseTierPrices(itemForm.tierText)
      await bulk.mutateAsync({
        priceListId: data.id,
        currency: data.currency,
        items: [{
          skuId: sku,
          basePrice,
          tierPrices: tierPrices.length > 0 ? tierPrices : undefined,
        }],
      })
      setItemForm({ skuId: '', basePrice: '', tierText: '' })
      swalToastSuccess('آیتم به‌روزرسانی شد.')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در به‌روزرسانی آیتم.'))
    }
  }

  const handleParsePaste = () => {
    try {
      const parsed = parsePasteGrid(pasteText)
      setPreview(parsed)
      if (parsed.length === 0) {
        swalToastError('هیچ ردیفی قابل پردازش نبود.')
      }
    } catch (err: any) {
      swalToastError(err?.message || 'خطا در پردازش متن.')
    }
  }

  const handleApplyPaste = async () => {
    if (!data) return
    if (preview.length === 0) {
      swalToastError('ابتدا متن را پردازش کنید.')
      return
    }
    try {
      await bulk.mutateAsync({
        priceListId: data.id,
        currency: data.currency,
        items: preview.map(p => ({
          skuId: p.skuId,
          basePrice: p.basePrice,
          tierPrices: p.tierPrices.length > 0 ? p.tierPrices : undefined,
        })),
      })
      setPasteText('')
      setPreview([])
      swalToastSuccess('آیتم‌ها به‌روزرسانی شدند.')
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در به‌روزرسانی آیتم‌ها.'))
    }
  }

  const handleFillItem = (item: PriceList['items'][number]) => {
    setItemForm({
      skuId: item.skuId,
      basePrice: String(item.basePrice),
      tierText: formatTierPrices(item.tierPrices),
    })
  }

  return (
    <div className="space-y-4">
      <PageHeader title="مدیریت لیست قیمت" actions={
        <button className="btn-red px-3 py-1.5 rounded" onClick={handleDelete}>حذف لیست</button>
      }>
        {data ? `نمایش و ویرایش لیست: ${data.name}` : '...'}
      </PageHeader>

      {isLoading || !data ? <Spinner /> : (
        <div className="space-y-4">
          <div className="card p-4">
            <h3 className="font-semibold mb-3">مشخصات لیست</h3>
            <form className="space-y-3" onSubmit={handleListSave}>
              <div>
                <label className="label">نام</label>
                <input className="input" value={listForm.name} onChange={e => setListForm(f => ({ ...f, name: e.target.value }))} />
              </div>
              <div>
                <label className="label">ارز</label>
                <input className="input" value={listForm.currency} onChange={e => setListForm(f => ({ ...f, currency: e.target.value }))} />
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className="label">اعتبار از</label>
                  <JalaliDateTimePicker value={listForm.validFrom} onChange={(v) => setListForm(f => ({ ...f, validFrom: v }))} clearable />
                </div>
                <div>
                  <label className="label">اعتبار تا</label>
                  <JalaliDateTimePicker value={listForm.validTo} onChange={(v) => setListForm(f => ({ ...f, validTo: v }))} clearable />
                </div>
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={listForm.isActive} onChange={e => setListForm(f => ({ ...f, isActive: e.target.checked }))} />
                فعال باشد
              </label>
              <button type="submit" className="btn" disabled={update.isPending}>
                {update.isPending ? 'در حال ذخیره...' : 'ذخیره تغییرات'}
              </button>
            </form>
          </div>

          <div className="card p-4">
            <div className="flex items-center justify-between mb-3">
              <h3 className="font-semibold">آیتم‌های لیست قیمت</h3>
              <div className="text-xs text-gray-600">
                آخرین اعتبار: {formatJalaliDate(data.validFrom)} تا {formatJalaliDate(data.validTo)}
              </div>
            </div>
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm text-center">
                <thead>
                  <tr className="bg-gray-100">
                    <th className="p-2">محصول/واریانت</th>
                    <th className="p-2">قیمت پایه</th>
                    <th className="p-2">قیمت‌های پله‌ای</th>
                    <th className="p-2">عملیات</th>
                  </tr>
                </thead>
                <tbody>
                  {items.length === 0 && (
                    <tr className="border-b last:border-0">
                      <td colSpan={4} className="p-3 text-sm text-gray-600">آیتمی ثبت نشده است.</td>
                    </tr>
                  )}
                  {items.map(item => (
                    <tr key={item.id} className="border-b last:border-0">
                      <td className="p-2">
                        {formatSkuLabel(item.skuId) ? (
                          <div>
                            <div className="font-semibold">{formatSkuLabel(item.skuId)}</div>
                            <div className="text-xs text-gray-600">{item.skuId}</div>
                          </div>
                        ) : item.skuId}
                      </td>
                      <td className="p-2">{item.basePrice}</td>
                      <td className="p-2">{formatTierPrices(item.tierPrices)}</td>
                      <td className="p-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => handleFillItem(item)}>
                          ویرایش
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <div className="card p-4">
              <h3 className="font-semibold mb-3">افزودن/ویرایش آیتم</h3>
              <form className="space-y-3" onSubmit={handleItemUpsert}>
                <div>
                  <label className="label">محصول/واریانت</label>
                  <SkuPicker
                    label="انتخاب محصول/واریانت"
                    value={itemForm.skuId}
                    onChange={(next) => setItemForm(f => ({ ...f, skuId: next.skuId }))}
                    showLabel={false}
                  />
                </div>
                <div>
                  <label className="label">قیمت پایه</label>
                  <input className="input" type="number" value={itemForm.basePrice} onChange={e => setItemForm(f => ({ ...f, basePrice: e.target.value }))} />
                </div>
                <div>
                  <label className="label">قیمت‌های پله‌ای (مثال: 5:95000;10:90000)</label>
                  <input className="input" value={itemForm.tierText} onChange={e => setItemForm(f => ({ ...f, tierText: e.target.value }))} />
                </div>
                <button className="btn w-full" type="submit" disabled={bulk.isPending}>
                  {bulk.isPending ? 'در حال ذخیره...' : 'ثبت آیتم'}
                </button>
              </form>
            </div>

            <div className="card p-4">
              <h3 className="font-semibold mb-3">درج سریع (چسباندن جدول)</h3>
              <p className="text-xs text-gray-600 mb-2">هر خط: SKU،قیمت یا SKU،قیمت،پله‌ها</p>
              <textarea
                className="input min-h-[140px]"
                value={pasteText}
                onChange={e => setPasteText(e.target.value)}
                placeholder="SKU123,120000
SKU456,95000,5:90000;10:85000"
              />
              <div className="flex items-center gap-2 mt-2">
                <button className="btn-secondary px-3 py-1.5 rounded" onClick={handleParsePaste}>پیش‌نمایش</button>
                <button className="btn px-3 py-1.5 rounded" onClick={handleApplyPaste} disabled={bulk.isPending}>اعمال</button>
              </div>
              {preview.length > 0 && (
                <div className="mt-3 overflow-x-auto">
                  <table className="min-w-full text-xs text-center">
                    <thead>
                      <tr className="bg-gray-100">
                        <th className="p-2">محصول/واریانت</th>
                        <th className="p-2">قیمت پایه</th>
                        <th className="p-2">پله‌ها</th>
                      </tr>
                    </thead>
                    <tbody>
                      {preview.map((p, idx) => (
                        <tr key={`${p.skuId}-${idx}`} className="border-b last:border-0">
                          <td className="p-2">
                            {formatSkuLabel(p.skuId) ? (
                              <div>
                                <div className="font-semibold">{formatSkuLabel(p.skuId)}</div>
                                <div className="text-xs text-gray-600">{p.skuId}</div>
                              </div>
                            ) : p.skuId}
                          </td>
                          <td className="p-2">{p.basePrice}</td>
                          <td className="p-2">{formatTierPrices(p.tierPrices)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

function parsePasteGrid(input: string): ParsedItem[] {
  const lines = input.split(/\r?\n/).map(l => l.trim()).filter(Boolean)
  const out: ParsedItem[] = []
  for (const line of lines) {
    const cols = line.split(/[\t,]/).map(c => c.trim()).filter(Boolean)
    if (cols.length < 2) continue
    const skuId = cols[0]
    const basePrice = parseNumber(cols[1])
    if (!skuId || basePrice === null) continue
    const tierText = cols.slice(2).join(',')
    const tierPrices = tierText ? parseTierPrices(tierText) : []
    out.push({ skuId, basePrice, tierPrices })
  }
  return out
}

function parseTierPrices(input: string): TierPrice[] {
  const text = input.trim()
  if (!text) return []
  const parts = text.split(/[;,\n]+/).map(p => p.trim()).filter(Boolean)
  const tiers: TierPrice[] = []
  for (const part of parts) {
    const [minRaw, priceRaw] = part.split(/[:=]/).map(p => p.trim())
    const minQty = minRaw ? Number.parseInt(minRaw, 10) : NaN
    const unitPrice = priceRaw ? Number.parseFloat(priceRaw) : NaN
    if (!Number.isFinite(minQty) || !Number.isFinite(unitPrice)) {
      throw new Error('فرمت قیمت پله‌ای معتبر نیست.')
    }
    tiers.push({ minQty, unitPrice })
  }
  return tiers
}

function parseNumber(input: string): number | null {
  const value = Number.parseFloat(String(input).replace(/,/g, '.'))
  return Number.isFinite(value) ? value : null
}

function formatTierPrices(tiers: TierPrice[] | undefined) {
  if (!tiers || tiers.length === 0) return '-'
  return tiers.map(t => `${t.minQty}:${t.unitPrice}`).join('؛ ')
}

function toLocalDateTime(value: string) {
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}
