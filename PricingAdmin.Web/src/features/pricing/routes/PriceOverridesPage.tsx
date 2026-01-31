import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { getApiErrorMessage } from '../api'
import { listCatalogStores } from '../catalogApi'
import type { CatalogStoreListItem } from '../catalogTypes'
import { useCreateOverride, useDeleteOverride, useOverrides, usePriceList, usePriceLists, useUpdateOverride } from '../queries'
import type { OverrideScope, OverrideType, PriceList, PriceListItem, PriceOverride } from '../types'
import { SkuPicker } from '../components/SkuPicker'
import { formatSkuLabel } from '../catalogSkuLabels'

type FilterState = {
  scopeType: OverrideScope | ''
  scopeId: string
  skuId: string
  activeAt: string
}

type FormState = {
  scopeType: OverrideScope
  scopeId: string
  skuId: string
  overrideType: OverrideType
  value: string
  currency: string
  validFrom: string
  validTo: string
  priority: string
  stackingGroup: string
}

const emptyForm = (): FormState => ({
  scopeType: 'Site',
  scopeId: '',
  skuId: '',
  overrideType: 'PercentOff',
  value: '',
  currency: 'IRR',
  validFrom: '',
  validTo: '',
  priority: '0',
  stackingGroup: '',
})

export function PriceOverridesPage() {
  const [filters, setFilters] = useState<FilterState>({ scopeType: '', scopeId: '', skuId: '', activeAt: '' })
  const { data, isLoading } = useOverrides({
    scopeType: filters.scopeType || undefined,
    scopeId: filters.scopeId || undefined,
    skuId: filters.skuId || undefined,
  })
  const { data: priceLists } = usePriceLists()
  const activePriceList = useMemo(() => pickActivePriceList(priceLists), [priceLists])
  const { data: priceList } = usePriceList(activePriceList?.id)
  const priceIndex = useMemo(() => buildPriceIndex(priceList), [priceList])

  const [catalogStores, setCatalogStores] = useState<CatalogStoreListItem[]>([])
  const storeIndex = useMemo(() => new Map(catalogStores.map(s => [s.id, s])), [catalogStores])

  const create = useCreateOverride()
  const del = useDeleteOverride()
  const [editing, setEditing] = useState<PriceOverride | null>(null)
  const update = useUpdateOverride(editing?.id || '')
  const [form, setForm] = useState<FormState>(() => emptyForm())
  const [modalOpen, setModalOpen] = useState(false)

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

  const filtered = useMemo(() => {
    if (!data) return []
    if (!filters.activeAt) return data
    const target = new Date(filters.activeAt)
    if (Number.isNaN(target.getTime())) return data
    return data.filter(o => {
      const fromOk = !o.validFrom || new Date(o.validFrom) <= target
      const toOk = !o.validTo || new Date(o.validTo) >= target
      return fromOk && toOk
    })
  }, [data, filters.activeAt])

  const duplicatePriorityKeys = useMemo(() => {
    const counts = new Map<string, number>()
    filtered.forEach(o => {
      const key = buildOverridePriorityKey(o)
      counts.set(key, (counts.get(key) ?? 0) + 1)
    })
    const dupes = new Set<string>()
    counts.forEach((count, key) => {
      if (count > 1) dupes.add(key)
    })
    return dupes
  }, [filtered])


  const stackingGroupSuggestions = useMemo(() => {
    const base = ['Product', 'Category', 'Seasonal', 'Bundle', 'Cashback', 'Discount', 'PriceOverride']
    const set = new Set<string>(base)
    ;(data ?? []).forEach(o => {
      const group = (o.stackingGroup || '').trim()
      if (group) set.add(group)
    })
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'fa'))
  }, [data])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm())
    setModalOpen(true)
  }

  const startEdit = (o: PriceOverride) => {
    setEditing(o)
    setForm({
      scopeType: o.scopeType,
      scopeId: o.scopeId,
      skuId: o.skuId,
      overrideType: o.overrideType,
      value: String(o.value),
      currency: o.currency,
      validFrom: o.validFrom ? toLocalDateTime(o.validFrom) : '',
      validTo: o.validTo ? toLocalDateTime(o.validTo) : '',
      priority: String(o.priority ?? 0),
      stackingGroup: o.stackingGroup ?? '',
    })
    setModalOpen(true)
  }

  const resetForm = () => {
    setEditing(null)
    setForm(emptyForm())
    setModalOpen(false)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()

    if (!form.skuId.trim() || !form.scopeId.trim()) {
      swalToastError('شناسه دامنه و SKU ضروری است.')
      return
    }
    if (!form.stackingGroup.trim()) {
      swalToastError('گروه تجمیع (StackingGroup) ضروری است.')
      return
    }

    const payload = {
      overrideType: form.overrideType,
      value: Number.parseFloat(form.value || '0'),
      currency: form.currency.trim(),
      validFrom: form.validFrom || null,
      validTo: form.validTo || null,
      priority: Number.parseInt(form.priority || '0', 10) || 0,
      stackingGroup: form.stackingGroup.trim(),
    }
    const targetSku = form.skuId.trim()

    try {
      if (editing) {
        await update.mutateAsync(payload)
        swalToastSuccess('قیمت ویژه بروزرسانی شد.')
      } else {
        await create.mutateAsync({
          scopeType: form.scopeType,
          scopeId: form.scopeId.trim(),
          skuId: targetSku,
          ...payload,
        })
        swalToastSuccess('قیمت ویژه ایجاد شد.')
      }
      setFilters(prev => ({ ...prev, skuId: targetSku }))
      resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ذخیره قیمت ویژه.'))
    }
  }

  const handleDelete = async (id: string) => {
    const ok = await swalConfirm({
      title: 'حذف قیمت ویژه',
      text: 'آیا از حذف قیمت ویژه اطمینان دارید؟',
      icon: 'warning',
      confirmText: 'بله',
      cancelText: 'خیر',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      swalToastSuccess('قیمت ویژه حذف شد.')
      if (editing?.id === id) resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در حذف قیمت ویژه.'))
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="قیمت‌های ویژه"
        actions={(
          <button className="btn" onClick={openCreate}>
            ایجاد قیمت ویژه
          </button>
        )}
      >
        اول کاربر، بعد سایت؛ priority بالاتر مقدم است.
      </PageHeader>

      <div className="card p-4 space-y-3">
        <h3 className="font-semibold">فیلترها</h3>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div>
            <label className="label">نوع دامنه</label>
            <select className="input" value={filters.scopeType} onChange={e => setFilters(f => ({ ...f, scopeType: e.target.value as any }))}>
              <option value="">همه</option>
              <option value="Site">سایت</option>
              <option value="User">کاربر (نیازمند UserId)</option>
            </select>
          </div>
          <div>
            <label className="label">شناسه دامنه</label>
            <input className="input" value={filters.scopeId} onChange={e => setFilters(f => ({ ...f, scopeId: e.target.value }))} />
          </div>
          <div>
            <label className="label">SKU</label>
            <SkuPicker
              label="انتخاب محصول/واریانت"
              value={filters.skuId}
              onChange={(next) => setFilters(f => ({ ...f, skuId: next.skuId }))}
              showLabel={false}
            />
          </div>
          <div>
            <label className="label">فعال در تاریخ</label>
            <JalaliDateTimePicker value={filters.activeAt} onChange={(v) => setFilters(f => ({ ...f, activeAt: v }))} clearable />
          </div>
        </div>
        <div className="text-xs text-gray-600">
          بخش «کاربر» بدون نیاز به پیاده‌سازی User BC هم قابل استفاده است (با وارد کردن UserId)، اما فعلاً توصیه می‌شود از قیمت ویژه سایت استفاده کنید.
        </div>
      </div>

      <div className="card p-4">
        {isLoading ? <Spinner /> : (
          <div className="overflow-x-auto">
            {duplicatePriorityKeys.size > 0 && (
              <div className="mb-3 rounded-md border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
                هشدار: برای برخی رکوردها، اولویت تکراری در همان SKU و همان دامنه وجود دارد و ممکن است اعمال نهایی غیرقطعی باشد.
              </div>
            )}
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">نوع</th>
                  <th className="p-2">شناسه دامنه</th>
                  <th className="p-2">محصول / SKU</th>
                  <th className="p-2">نوع قیمت ویژه</th>
                  <th className="p-2">مقدار</th>
                  <th className="p-2">قیمت نهایی</th>
                  <th className="p-2">گروه</th>
                  <th className="p-2">اولویت</th>
                  <th className="p-2">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length === 0 && (
                  <tr className="border-b last:border-0">
                    <td colSpan={8} className="p-3 text-sm text-gray-600">موردی یافت نشد.</td>
                  </tr>
                )}
                  {filtered.map(o => (
                    (() => {
                      const basePrice = priceIndex.get(normalizeSku(o.skuId))?.basePrice
                      const finalPrice = computeOverrideFinalPrice(basePrice, o, priceList?.currency)
                      const finalLabel = finalPrice == null ? '-' : `${formatNumber(finalPrice)} ${currencyLabel(priceList?.currency)}`
                      const finalTooltip = buildOverrideFinalTooltip(basePrice, o, finalPrice, priceList?.currency)

                      return (
                    <tr key={o.id} className="border-b last:border-0">
                      <td className="p-2">{o.scopeType === 'Site' ? 'سایت' : 'کاربر'}</td>
                      <td className="p-2">
                        {o.scopeType === 'Site'
                          ? (storeIndex.get(o.scopeId)?.name ?? o.scopeId)
                        : o.scopeId}
                    </td>
                    <td className="p-2">
                      {formatSkuLabel(o.skuId) ? (
                        <div>
                          <div className="font-semibold">{formatSkuLabel(o.skuId)}</div>
                          <div className="text-xs text-gray-600">{o.skuId}</div>
                        </div>
                      ) : o.skuId}
                    </td>
                    <td className="p-2">{overrideTypeLabel(o.overrideType)}</td>
                    <td className="p-2">{o.value}</td>
                    <td className="p-2">
                      <div className="flex items-center justify-center gap-1">
                        <span>{finalLabel}</span>
                        <span
                          className="inline-flex h-4 w-4 items-center justify-center rounded-full border border-slate-300 text-[10px] text-slate-600 cursor-help"
                          title={finalTooltip}
                        >
                          i
                        </span>
                      </div>
                    </td>
                    <td className="p-2">{o.stackingGroup ?? '-'}</td>
                    <td className="p-2">
                      <div className="flex flex-col items-center gap-1">
                        <span>{o.priority}</span>
                        {duplicatePriorityKeys.has(buildOverridePriorityKey(o)) && (
                          <span className="text-xs text-amber-600">اولویت تکراری</span>
                        )}
                      </div>
                    </td>
                    <td className="p-2">
                      <div className="flex items-center justify-center gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => startEdit(o)}>ویرایش</button>
                        <button className="btn-red px-3 py-1.5 rounded" onClick={() => handleDelete(o.id)}>حذف</button>
                      </div>
                    </td>
                  </tr>
                      )
                    })()
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modalOpen && (
        <OverrideModal
          editing={editing}
          form={form}
          setForm={setForm}
          onClose={resetForm}
          onSubmit={handleSubmit}
          isSaving={create.isPending || update.isPending}
          stores={catalogStores}
          stackingGroupSuggestions={stackingGroupSuggestions}
        />
      )}
    </div>
  )
}

function overrideTypeLabel(t: OverrideType) {
  switch (t) {
    case 'PercentOff':
      return 'ویژه درصدی'
    case 'AmountOff':
      return 'ویژه ریالی'
    case 'FixedPrice':
      return 'قیمت ثابت'
    default:
      return t
  }
}

function OverrideModal(props: {
  editing: PriceOverride | null
  form: FormState
  setForm: (next: FormState | ((prev: FormState) => FormState)) => void
  onClose: () => void
  onSubmit: (e: FormEvent) => void
  isSaving: boolean
  stores: CatalogStoreListItem[]
  stackingGroupSuggestions: string[]
}) {
  const scopeIdLabel = props.form.scopeType === 'User' ? 'شناسه کاربر (UserId)' : 'سایت'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={props.onClose} />
      <div className="relative w-[96vw] max-w-3xl rounded-2xl bg-white shadow-xl">
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div>
            <div className="text-lg font-semibold">{props.editing ? 'ویرایش قیمت ویژه' : 'ایجاد قیمت ویژه'}</div>
            <div className="text-xs text-gray-500">قیمت ویژه برای یک SKU در یک سایت/کاربر.</div>
          </div>
          <button className="btn-secondary px-4 py-2.5 rounded-lg" onClick={props.onClose}>بستن</button>
        </div>

        <form className="p-6 space-y-4" onSubmit={props.onSubmit}>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">نوع دامنه</label>
              <select
                className="input"
                value={props.form.scopeType}
                onChange={e => props.setForm(f => ({ ...f, scopeType: e.target.value as OverrideScope }))}
                disabled={!!props.editing}
              >
                <option value="Site">سایت</option>
                <option value="User">کاربر (نیازمند UserId)</option>
              </select>
              <div className="text-xs text-gray-500 mt-1">حالت کاربر فعلاً با وارد کردن دستی UserId قابل استفاده است.</div>
            </div>
            <div>
              <label className="label">{scopeIdLabel}</label>
              {props.form.scopeType === 'Site' ? (
                <>
                  <select
                    className="input"
                    value={props.form.scopeId}
                    onChange={e => props.setForm(f => ({ ...f, scopeId: e.target.value }))}
                    disabled={!!props.editing}
                  >
                    <option value="">انتخاب سایت</option>
                    {props.form.scopeId && !props.stores.some(s => s.id === props.form.scopeId) && (
                      <option value={props.form.scopeId}>شناسه فعلی: {props.form.scopeId}</option>
                    )}
                    {props.stores.map(s => (
                      <option key={s.id} value={s.id}>{s.name}</option>
                    ))}
                  </select>

                  {props.stores.length === 0 && (
                    <div className="mt-2">
                      <label className="label text-xs">یا وارد کردن دستی SiteId</label>
                      <input
                        className="input"
                        value={props.form.scopeId}
                        onChange={e => props.setForm(f => ({ ...f, scopeId: e.target.value }))}
                        disabled={!!props.editing}
                        placeholder="GUID"
                      />
                      <div className="text-xs text-gray-500 mt-1">
                        اگر Catalog.Api اجرا نباشد/پورت اشتباه باشد یا توکن ادمین تنظیم نشده باشد، لیست سایت‌ها نمایش داده نمی‌شود.
                      </div>
                    </div>
                  )}
                </>
              ) : (
                <input className="input" value={props.form.scopeId} onChange={e => props.setForm(f => ({ ...f, scopeId: e.target.value }))} disabled={!!props.editing} />
              )}
            </div>
          </div>

          <div>
            <label className="label">SKU</label>
            <SkuPicker
              label="انتخاب محصول/واریانت"
              value={props.form.skuId}
              onChange={(next) => props.setForm(f => ({ ...f, skuId: next.skuId }))}
              disabled={!!props.editing}
              showLabel={false}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">نوع قیمت ویژه</label>
              <select className="input" value={props.form.overrideType} onChange={e => props.setForm(f => ({ ...f, overrideType: e.target.value as OverrideType }))}>
                <option value="PercentOff">ویژه درصدی</option>
                <option value="AmountOff">ویژه ریالی</option>
                <option value="FixedPrice">قیمت ثابت</option>
              </select>
            </div>
            <div>
              <label className="label">مقدار</label>
              <input className="input" type="number" value={props.form.value} onChange={e => props.setForm(f => ({ ...f, value: e.target.value }))} />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">ارز</label>
              <input className="input" value={props.form.currency} onChange={e => props.setForm(f => ({ ...f, currency: e.target.value }))} />
            </div>
            <div>
              <label className="label">اولویت</label>
              <input className="input" type="number" value={props.form.priority} onChange={e => props.setForm(f => ({ ...f, priority: e.target.value }))} />
            </div>
          </div>

          <div>
            <label className="label">گروه انباشت (StackingGroup)</label>
            <select
              className="input"
              value={props.form.stackingGroup}
              onChange={e => props.setForm(f => ({ ...f, stackingGroup: e.target.value }))}
            >
              <option value="">انتخاب گروه</option>
              {props.form.stackingGroup && !props.stackingGroupSuggestions.includes(props.form.stackingGroup) && (
                <option value={props.form.stackingGroup}>{props.form.stackingGroup}</option>
              )}
              {props.stackingGroupSuggestions.map(group => (
                <option key={group} value={group}>{group}</option>
              ))}
            </select>
            <div className="text-xs text-gray-500 mt-1">
              گروه انباشت مشخص می‌کند در حالت «بهترین هر گروه»، کدام قیمت ویژه از این گروه اعمال شود.
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="label">شروع</label>
              <JalaliDateTimePicker value={props.form.validFrom} onChange={(v) => props.setForm(f => ({ ...f, validFrom: v }))} clearable />
            </div>
            <div>
              <label className="label">پایان</label>
              <JalaliDateTimePicker value={props.form.validTo} onChange={(v) => props.setForm(f => ({ ...f, validTo: v }))} clearable />
            </div>
          </div>

          <div className="flex items-center gap-2 justify-end">
            <button type="button" className="btn-secondary px-4 py-2 rounded-lg" onClick={props.onClose} disabled={props.isSaving}>انصراف</button>
            <button type="submit" className="btn px-5 py-2 rounded-lg" disabled={props.isSaving}>
              {props.editing ? 'ذخیره تغییرات' : 'ایجاد'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}


function normalizeSku(value: string) {
  return value.trim().toLowerCase()
}

function buildOverridePriorityKey(o: PriceOverride) {
  return `${o.scopeType}|${o.scopeId}|${normalizeSku(o.skuId)}|${o.priority ?? 0}`
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

function buildPriceIndex(list?: PriceList | null) {
  const map = new Map<string, PriceListItem>()
  if (!list) return map
  for (const item of list.items || []) {
    map.set(normalizeSku(item.skuId), item)
  }
  return map
}

function computeOverrideFinalPrice(basePrice: number | undefined, override: PriceOverride, currency?: string) {
  if (basePrice == null || !Number.isFinite(basePrice)) return null
  if (!currency) return null
  if (override.currency && override.currency.toUpperCase() !== currency.toUpperCase()) return null

  const value = Number(override.value)
  if (!Number.isFinite(value)) return null
  const round = (n: number) => ((currency === 'IRR' || currency === 'IRT') ? Math.round(n) : Math.round(n * 100) / 100)

  switch (override.overrideType) {
    case 'FixedPrice':
      return round(Math.max(0, value))
    case 'PercentOff':
      return round(Math.max(0, basePrice * (1 - value / 100)))
    case 'AmountOff':
      return round(Math.max(0, basePrice - value))
    default:
      return null
  }
}

function formatNumber(value: number) {
  return value.toLocaleString('fa-IR')
}

function currencyLabel(currency?: string) {
  const c = (currency || '').toUpperCase()
  if (c === 'IRR') return 'ریال'
  if (c === 'IRT') return 'تومان'
  return c || ''
}

function buildOverrideFinalTooltip(
  basePrice: number | undefined,
  override: PriceOverride,
  finalPrice: number | null,
  currency?: string,
) {
  if (basePrice == null || !Number.isFinite(basePrice)) {
    return 'قیمت پایه برای این SKU در لیست قیمت فعال یافت نشد.'
  }

  const curLabel = currencyLabel(currency)
  const value = Number(override.value)
  const valueLabel = Number.isFinite(value)
    ? formatOverrideValue(override.overrideType, value, curLabel)
    : 'نامشخص'

  const baseLabel = `${formatNumber(basePrice)} ${curLabel}`.trim()
  const finalLabel = finalPrice == null ? 'نامشخص' : `${formatNumber(finalPrice)} ${curLabel}`.trim()

  return `قیمت پایه: ${baseLabel} | تغییر: ${valueLabel} | قیمت نهایی: ${finalLabel}`
}

function formatOverrideValue(type: OverrideType, value: number, curLabel: string) {
  switch (type) {
    case 'PercentOff':
      return `${value}%`
    case 'AmountOff':
      return `${formatNumber(value)} ${curLabel}`.trim()
    case 'FixedPrice':
      return `قیمت ثابت ${formatNumber(value)} ${curLabel}`.trim()
    default:
      return formatNumber(value)
  }
}

function toLocalDateTime(value: string) {
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}
