import type { BenefitDefinition, EligibilityDefinition } from '../types'
import { useEffect, useMemo, useState } from 'react'
import { swalToastError } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { SkuPicker } from './SkuPicker'
import { listCatalogBrands, listCatalogCategoryLeaves, listCatalogStores } from '../catalogApi'
import type { CatalogBrandListItem, CatalogCategoryLeaf, CatalogStoreListItem } from '../catalogTypes'

export type ConditionKind =
  | 'all'
  | 'product'
  | 'category'
  | 'brand'
  | 'tag'
  | 'site'
  | 'user'
  | 'seasonal'
  | 'bundle'
  | 'batchExpiryBefore'
  | 'minQty'
  | 'minSubtotal'

type ConditionState = {
  id: number
  kind: ConditionKind
  values: Record<string, string>
  list?: string
}

export type EligibilityBuilderState = {
  mode: 'single' | 'allOf' | 'anyOf'
  conditions: ConditionState[]
}

const conditionMeta: Record<ConditionKind, { label: string; container: string; badge: string }> = {
  all: { label: 'همه', container: 'border-slate-200 bg-slate-50/60', badge: 'bg-slate-200 text-slate-700' },
  product: { label: 'محصول (SKU)', container: 'border-emerald-200 bg-emerald-50/60', badge: 'bg-emerald-200 text-emerald-800' },
  category: { label: 'دسته‌بندی', container: 'border-sky-200 bg-sky-50/60', badge: 'bg-sky-200 text-sky-800' },
  brand: { label: 'برند', container: 'border-indigo-200 bg-indigo-50/60', badge: 'bg-indigo-200 text-indigo-800' },
  tag: { label: 'تگ', container: 'border-amber-200 bg-amber-50/60', badge: 'bg-amber-200 text-amber-800' },
  site: { label: 'سایت', container: 'border-teal-200 bg-teal-50/60', badge: 'bg-teal-200 text-teal-800' },
  user: { label: 'کاربر', container: 'border-rose-200 bg-rose-50/60', badge: 'bg-rose-200 text-rose-800' },
  seasonal: { label: 'فصلی', container: 'border-violet-200 bg-violet-50/60', badge: 'bg-violet-200 text-violet-800' },
  bundle: { label: 'باندل', container: 'border-orange-200 bg-orange-50/60', badge: 'bg-orange-200 text-orange-800' },
  batchExpiryBefore: { label: 'انقضای بچ', container: 'border-cyan-200 bg-cyan-50/60', badge: 'bg-cyan-200 text-cyan-800' },
  minQty: { label: 'حداقل تعداد', container: 'border-gray-200 bg-gray-50', badge: 'bg-gray-200 text-gray-700' },
  minSubtotal: { label: 'حداقل مبلغ', container: 'border-gray-200 bg-gray-50', badge: 'bg-gray-200 text-gray-700' },
}

const getConditionLabel = (kind: ConditionKind) => conditionMeta[kind]?.label ?? 'شرط'
const getConditionContainer = (kind: ConditionKind) => conditionMeta[kind]?.container ?? 'border-slate-200 bg-white'
const getConditionBadge = (kind: ConditionKind) => conditionMeta[kind]?.badge ?? 'bg-slate-200 text-slate-700'

export function makeDefaultEligibility(): EligibilityBuilderState {
  return {
    mode: 'single',
    conditions: [{ id: 1, kind: 'all', values: {} }],
  }
}

export function EligibilityBuilder({
  value,
  onChange,
}: {
  value: EligibilityBuilderState
  onChange: (next: EligibilityBuilderState) => void
}) {
  const conditions = value.conditions
  const [catalogStores, setCatalogStores] = useState<CatalogStoreListItem[]>([])
  const [catalogBrands, setCatalogBrands] = useState<CatalogBrandListItem[]>([])
  const [catalogCategories, setCatalogCategories] = useState<CatalogCategoryLeaf[]>([])
  const [storesLoading, setStoresLoading] = useState(false)
  const [brandsLoading, setBrandsLoading] = useState(false)
  const [categoriesLoading, setCategoriesLoading] = useState(false)

  useEffect(() => {
    let active = true
    const loadStores = async () => {
      setStoresLoading(true)
      try {
        const stores = await listCatalogStores()
        if (!active) return
        setCatalogStores(stores || [])
      } catch (err) {
        if (!active) return
        swalToastError('خطا در دریافت فهرست سایت‌ها از کاتالوگ.')
      } finally {
        if (active) setStoresLoading(false)
      }
    }
    void loadStores()
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    let active = true
    const loadBrands = async () => {
      setBrandsLoading(true)
      try {
        const brands = await listCatalogBrands()
        if (!active) return
        setCatalogBrands(brands || [])
      } catch (err) {
        if (!active) return
        swalToastError('خطا در دریافت فهرست برندها از کاتالوگ.')
      } finally {
        if (active) setBrandsLoading(false)
      }
    }
    void loadBrands()
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    let active = true
    const loadCategories = async () => {
      setCategoriesLoading(true)
      try {
        const categories = await listCatalogCategoryLeaves()
        if (!active) return
        setCatalogCategories(categories || [])
      } catch (err) {
        if (!active) return
        swalToastError('خطا در دریافت فهرست دسته‌بندی‌ها از کاتالوگ.')
      } finally {
        if (active) setCategoriesLoading(false)
      }
    }
    void loadCategories()
    return () => {
      active = false
    }
  }, [])

  const addCondition = () => {
    onChange({
      ...value,
      conditions: [...conditions, { id: Date.now(), kind: 'product', values: {}, list: '' }],
    })
  }

  const removeCondition = (id: number) => {
    const next = conditions.filter(c => c.id !== id)
    onChange({ ...value, conditions: next.length > 0 ? next : [{ id: Date.now(), kind: 'all', values: {} }] })
  }

  const updateCondition = (id: number, patch: Partial<ConditionState>) => {
    onChange({
      ...value,
      conditions: conditions.map(c => (c.id === id ? { ...c, ...patch } : c)),
    })
  }

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <div>
          <label className="label">حالت ترکیب</label>
          <select
            className="input"
            value={value.mode}
            onChange={e => onChange({ ...value, mode: e.target.value as EligibilityBuilderState['mode'] })}
          >
            <option value="single">تک شرط</option>
            <option value="allOf">همه شروط (AllOf)</option>
            <option value="anyOf">هرکدام از شروط (AnyOf)</option>
          </select>
        </div>
        <div className="md:col-span-2 text-xs text-gray-600 flex items-end">
          شرایط بر اساس نوع انتخابی ساخته می‌شوند. گزینه‌های minQty و minSubtotal در API موجود نیستند.
        </div>
      </div>

      {conditions.map((cond) => (
        <div key={cond.id} className={`border rounded-md p-3 space-y-2 ${getConditionContainer(cond.kind)}`}>
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <label className="label">نوع شرط</label>
              <span className={`text-xs px-2 py-0.5 rounded-full ${getConditionBadge(cond.kind)}`}>
                {getConditionLabel(cond.kind)}
              </span>
            </div>
            {conditions.length > 1 && (
              <button type="button" className="btn-red px-3 py-1.5 rounded" onClick={() => removeCondition(cond.id)}>
                حذف شرط
              </button>
            )}
          </div>
          <select
            className="input"
            value={cond.kind}
            onChange={e => updateCondition(cond.id, { kind: e.target.value as ConditionKind, values: {}, list: '' })}
          >
            <option value="all">همه</option>
            <option value="product">محصول (SKU)</option>
            <option value="category">دسته‌بندی</option>
            <option value="brand">برند</option>
            <option value="tag">تگ</option>
            <option value="site">سایت</option>
            <option value="user">کاربر</option>
            <option value="seasonal">فصلی</option>
            <option value="bundle">باندل (نیازمندی)</option>
            <option value="batchExpiryBefore">انقضای بچ قبل از تاریخ</option>
            <option value="minQty">حداقل تعداد (ناموجود در API)</option>
            <option value="minSubtotal">حداقل مبلغ (ناموجود در API)</option>
          </select>

          <ConditionFields
            condition={cond}
            onChange={(patch) => updateCondition(cond.id, patch)}
            stores={catalogStores}
            brands={catalogBrands}
            categories={catalogCategories}
            storesLoading={storesLoading}
            brandsLoading={brandsLoading}
            categoriesLoading={categoriesLoading}
          />
        </div>
      ))}

      {value.mode !== 'single' && (
        <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={addCondition}>
          افزودن شرط
        </button>
      )}
    </div>
  )
}

type IdListEditorProps<T> = {
  label: string
  helper?: string
  items: T[]
  loading: boolean
  value?: string
  onChange: (next: string) => void
  getId: (item: T) => string
  getLabel: (item: T) => string
  emptyHint?: string
  searchPlaceholder?: string
}

function IdListEditor<T>(props: IdListEditorProps<T>) {
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState('')
  const selectedIds = useMemo(() => parseList(props.value), [props.value])

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase()
    if (!term) return props.items
    return props.items.filter(item => props.getLabel(item).toLowerCase().includes(term))
  }, [props.items, props.getLabel, search])

  const addSelected = () => {
    const id = selectedId.trim()
    if (!id) return
    const next = selectedIds.includes(id) ? selectedIds : [...selectedIds, id]
    props.onChange(next.join('\n'))
    setSelectedId('')
  }

  const removeSelected = (id: string) => {
    props.onChange(selectedIds.filter(x => x !== id).join('\n'))
  }

  const labelForId = (id: string) => {
    const item = props.items.find(x => props.getId(x) === id)
    return item ? props.getLabel(item) : 'نامشخص'
  }

  return (
    <div className="space-y-2">
      <label className="label">{props.label}</label>
      <div className="border rounded-md p-3 bg-white space-y-2">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
          <input
            className="input"
            value={search}
            onChange={e => setSearch(e.target.value)}
            placeholder={props.searchPlaceholder || 'جستجو...'}
          />
          <select
            className="input"
            value={selectedId}
            onChange={e => setSelectedId(e.target.value)}
            disabled={props.loading || filtered.length === 0}
          >
            <option value="">
              {props.loading ? 'در حال دریافت...' : 'انتخاب کنید'}
            </option>
            {filtered.map(item => {
              const id = props.getId(item)
              return (
                <option key={id} value={id}>{props.getLabel(item)}</option>
              )
            })}
          </select>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={addSelected} disabled={!selectedId}>
            افزودن
          </button>
          {props.helper ? <div className="text-xs text-gray-600">{props.helper}</div> : null}
        </div>
        {selectedIds.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {selectedIds.map(id => (
              <span key={id} className="inline-flex items-center gap-1 rounded-full bg-slate-100 px-3 py-1 text-xs">
                <span>{labelForId(id)}</span>
                <button type="button" className="text-slate-500 hover:text-slate-700" onClick={() => removeSelected(id)}>
                  ×
                </button>
              </span>
            ))}
          </div>
        )}
        {!props.loading && props.items.length === 0 && props.emptyHint ? (
          <div className="text-xs text-gray-500">{props.emptyHint}</div>
        ) : null}
      </div>
    </div>
  )
}

function ConditionFields({
  condition,
  onChange,
  stores,
  brands,
  categories,
  storesLoading,
  brandsLoading,
  categoriesLoading,
}: {
  condition: ConditionState
  onChange: (patch: Partial<ConditionState>) => void
  stores: CatalogStoreListItem[]
  brands: CatalogBrandListItem[]
  categories: CatalogCategoryLeaf[]
  storesLoading: boolean
  brandsLoading: boolean
  categoriesLoading: boolean
}) {
  const setValue = (key: string, val: string) => onChange({ values: { ...condition.values, [key]: val } })
  const [productSkuToAdd, setProductSkuToAdd] = useState('')

  if (condition.kind === 'product') {
    const addSku = () => {
      const sku = productSkuToAdd.trim()
      if (!sku) {
        swalToastError('لطفاً یک محصول/واریانت انتخاب کنید.')
        return
      }
      const current = (condition.list || '').trim()
      const lines = current ? current.split(/\r?\n/).map(l => l.trim()).filter(Boolean) : []
      if (!lines.some(x => x.toLowerCase() === sku.toLowerCase())) lines.push(sku)
      onChange({ list: lines.join('\n') })
      setProductSkuToAdd('')
    }

    return (
      <div className="space-y-2">
        <div className="border rounded-md p-3 space-y-2">
          <SkuPicker
            label="افزودن محصول به شرط"
            value={productSkuToAdd}
            onChange={(next) => setProductSkuToAdd(next.skuId)}
          />
          <button type="button" className="btn-secondary px-3 py-1.5 rounded" onClick={addSku}>
            افزودن
          </button>
          <div className="text-xs text-gray-600">SKUهای انتخاب‌شده این شرط را می‌سازند.</div>
        </div>
        <div>
          <label className="label">SKUهای انتخاب‌شده (اختیاری)</label>
          <textarea className="input min-h-[80px]" value={condition.list || ''} onChange={e => onChange({ list: e.target.value })} />
        </div>
      </div>
    )
  }
  if (condition.kind === 'category') {
    return (
      <IdListEditor
        label="دسته‌بندی‌ها"
        helper="گزینه‌های انتخاب‌شده در این شرط لحاظ می‌شوند."
        items={categories}
        loading={categoriesLoading}
        value={condition.list}
        onChange={(next) => onChange({ list: next })}
        getId={(c) => c.id}
        getLabel={(c) => `${'↳ '.repeat(Math.max(0, c.depth || 0))}${c.name}${c.slug ? ` (${c.slug})` : ''}`}
        emptyHint="هیچ دسته‌بندی‌ای یافت نشد."
        searchPlaceholder="جستجو در دسته‌بندی‌ها..."
      />
    )
  }
  if (condition.kind === 'brand') {
    return (
      <IdListEditor
        label="برندها"
        helper="گزینه‌های انتخاب‌شده در این شرط لحاظ می‌شوند."
        items={brands}
        loading={brandsLoading}
        value={condition.list}
        onChange={(next) => onChange({ list: next })}
        getId={(b) => b.id}
        getLabel={(b) => b.name}
        emptyHint="هیچ برندی یافت نشد."
        searchPlaceholder="جستجو در برندها..."
      />
    )
  }
  if (condition.kind === 'tag') {
    return (
      <div>
        <label className="label">تگ‌ها (هر خط یک مقدار)</label>
        <textarea className="input min-h-[80px]" value={condition.list || ''} onChange={e => onChange({ list: e.target.value })} />
      </div>
    )
  }
  if (condition.kind === 'site') {
    return (
      <IdListEditor
        label="سایت‌ها"
        helper="سایت‌های انتخاب‌شده در این شرط لحاظ می‌شوند."
        items={stores}
        loading={storesLoading}
        value={condition.list}
        onChange={(next) => onChange({ list: next })}
        getId={(s) => s.id}
        getLabel={(s) => `${s.name}${s.domain ? ` (${s.domain})` : ''}`}
        emptyHint="هیچ سایتی یافت نشد."
        searchPlaceholder="جستجو در سایت‌ها..."
      />
    )
  }
  if (condition.kind === 'user') {
    return (
      <div>
        <label className="label">شناسه کاربر (هر خط یک مقدار)</label>
        <textarea className="input min-h-[80px]" value={condition.list || ''} onChange={e => onChange({ list: e.target.value })} />
      </div>
    )
  }
  if (condition.kind === 'seasonal') {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div>
          <label className="label">شروع</label>
          <JalaliDateTimePicker value={condition.values.from || ''} onChange={(v) => setValue('from', v)} clearable />
        </div>
        <div>
          <label className="label">پایان</label>
          <JalaliDateTimePicker value={condition.values.to || ''} onChange={(v) => setValue('to', v)} clearable />
        </div>
      </div>
    )
  }
  if (condition.kind === 'bundle') {
    return (
      <div>
        <label className="label">نیازمندی‌ها (هر خط: SKU,Qty)</label>
        <textarea className="input min-h-[80px]" value={condition.list || ''} onChange={e => onChange({ list: e.target.value })} />
      </div>
    )
  }
  if (condition.kind === 'batchExpiryBefore') {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div>
          <label className="label">تاریخ (انقضا)</label>
          <JalaliDateTimePicker value={condition.values.date || ''} onChange={(v) => setValue('date', v)} clearable />
        </div>
        <div>
          <label className="label">در بازه X روز قبل (اختیاری)</label>
          <input className="input" type="number" value={condition.values.withinDays || ''} onChange={e => setValue('withinDays', e.target.value)} />
        </div>
      </div>
    )
  }
  if (condition.kind === 'minQty' || condition.kind === 'minSubtotal') {
    return (
      <div className="text-xs text-gray-600">
        این شرط در API پشتیبانی نمی‌شود.
      </div>
    )
  }
  return null
}

export function buildEligibility(def: EligibilityBuilderState): EligibilityDefinition | null {
  const validConditions: EligibilityDefinition[] = []

  for (const cond of def.conditions) {
    if (cond.kind === 'minQty' || cond.kind === 'minSubtotal') {
      swalToastError('این شرط در API پشتیبانی نمی‌شود.')
      return null
    }
    validConditions.push(convertCondition(cond))
  }

  if (def.mode === 'single') return validConditions[0] ?? { kind: 'all' }
  if (def.mode === 'allOf') return { kind: 'allOf', conditions: validConditions }
  return { kind: 'anyOf', conditions: validConditions }
}

function convertCondition(cond: ConditionState): EligibilityDefinition {
  switch (cond.kind) {
    case 'product':
      return { kind: 'product', skuIds: parseList(cond.list) }
    case 'category':
      return { kind: 'category', categoryIds: parseList(cond.list) }
    case 'brand':
      return { kind: 'brand', brandIds: parseList(cond.list) }
    case 'tag':
      return { kind: 'tag', tags: parseList(cond.list) }
    case 'site':
      return { kind: 'site', siteIds: parseList(cond.list) }
    case 'user':
      return { kind: 'user', userIds: parseList(cond.list) }
    case 'seasonal':
      return { kind: 'seasonal', from: cond.values.from || null, to: cond.values.to || null }
    case 'bundle':
      return { kind: 'bundle', requirements: parseBundleRequirements(cond.list) }
    case 'batchExpiryBefore':
      return {
        kind: 'batchExpiryBefore',
        date: cond.values.date || null,
        withinDays: cond.values.withinDays ? Number.parseInt(cond.values.withinDays, 10) : null,
      }
    case 'all':
    default:
      return { kind: 'all' }
  }
}

function parseList(input?: string) {
  if (!input) return []
  return input.split(/\r?\n|,/).map(s => s.trim()).filter(Boolean)
}

function parseBundleRequirements(input?: string) {
  if (!input) return []
  const lines = input.split(/\r?\n/).map(l => l.trim()).filter(Boolean)
  return lines.map(line => {
    const [sku, qtyRaw] = line.split(/[,\t]/).map(p => p.trim())
    const qty = qtyRaw ? Number.parseInt(qtyRaw, 10) : 1
    return { skuId: sku, qty }
  }).filter(r => r.skuId)
}

export type BenefitBuilderState =
  | { kind: 'percentOff'; percent: string }
  | { kind: 'amountOff'; amount: string; currency: string }
  | { kind: 'fixedPrice'; price: string; currency: string }
  | { kind: 'cashbackPercent'; percent: string }
  | { kind: 'cashbackAmount'; amount: string; currency: string }
  | { kind: 'bundleFixedPrice'; requiredItems: string; bundlePrice: string; currency: string }
  | { kind: 'buyXGetY'; buySkuId: string; buyQty: string; getSkuId: string; getQty: string }

export function makeDefaultBenefit(): BenefitBuilderState {
  return { kind: 'percentOff', percent: '10' }
}

export function BenefitBuilder({
  value,
  onChange,
}: {
  value: BenefitBuilderState
  onChange: (next: BenefitBuilderState) => void
}) {
  const [bundleSkuToAdd, setBundleSkuToAdd] = useState('')
  const [bundleQtyToAdd, setBundleQtyToAdd] = useState('1')

  const addBundleItem = () => {
    if (value.kind !== 'bundleFixedPrice') return
    const sku = bundleSkuToAdd.trim()
    if (!sku) {
      swalToastError('لطفاً یک محصول/واریانت انتخاب کنید.')
      return
    }
    const qty = Number.parseInt(bundleQtyToAdd || '1', 10)
    const safeQty = Number.isFinite(qty) && qty > 0 ? qty : 1
    const current = (value.requiredItems || '').trim()
    const nextLine = `${sku},${safeQty}`
    const nextText = current ? `${current}\n${nextLine}` : nextLine
    onChange({ ...value, requiredItems: nextText })
    setBundleSkuToAdd('')
    setBundleQtyToAdd('1')
  }

  return (
    <div className="space-y-2">
      <div>
        <label className="label">نوع مزیت</label>
        <select
          className="input"
          value={value.kind}
          onChange={e => {
            const kind = e.target.value as BenefitBuilderState['kind']
            onChange(makeBenefitState(kind))
          }}
        >
          <option value="percentOff">درصد تخفیف</option>
          <option value="amountOff">مبلغ تخفیف</option>
          <option value="fixedPrice">قیمت ثابت</option>
          <option value="cashbackPercent">کش‌بک درصدی</option>
          <option value="cashbackAmount">کش‌بک ریالی</option>
          <option value="bundleFixedPrice">باندل قیمت ثابت</option>
          <option value="buyXGetY">بخر X ببر Y</option>
        </select>
      </div>

      {value.kind === 'percentOff' && (
        <div>
          <label className="label">درصد</label>
          <input className="input" type="number" value={value.percent} onChange={e => onChange({ ...value, percent: e.target.value })} />
        </div>
      )}

      {value.kind === 'amountOff' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="label">مبلغ</label>
            <input className="input" type="number" value={value.amount} onChange={e => onChange({ ...value, amount: e.target.value })} />
          </div>
          <div>
            <label className="label">ارز</label>
            <input className="input" value={value.currency} onChange={e => onChange({ ...value, currency: e.target.value })} />
          </div>
        </div>
      )}

      {value.kind === 'fixedPrice' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="label">قیمت</label>
            <input className="input" type="number" value={value.price} onChange={e => onChange({ ...value, price: e.target.value })} />
          </div>
          <div>
            <label className="label">ارز</label>
            <input className="input" value={value.currency} onChange={e => onChange({ ...value, currency: e.target.value })} />
          </div>
        </div>
      )}

      {value.kind === 'cashbackPercent' && (
        <div>
          <label className="label">درصد کش‌بک</label>
          <input className="input" type="number" value={value.percent} onChange={e => onChange({ ...value, percent: e.target.value })} />
        </div>
      )}

      {value.kind === 'cashbackAmount' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="label">مبلغ کش‌بک</label>
            <input className="input" type="number" value={value.amount} onChange={e => onChange({ ...value, amount: e.target.value })} />
          </div>
          <div>
            <label className="label">ارز</label>
            <input className="input" value={value.currency} onChange={e => onChange({ ...value, currency: e.target.value })} />
          </div>
        </div>
      )}

      {value.kind === 'bundleFixedPrice' && (
        <div className="space-y-2">
          <div className="border rounded-md p-3 space-y-2">
            <SkuPicker
              label="افزودن کالا به باندل"
              value={bundleSkuToAdd}
              onChange={(next) => setBundleSkuToAdd(next.skuId)}
            />
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div>
                <label className="label">تعداد موردنیاز</label>
                <input className="input" type="number" min={1} value={bundleQtyToAdd} onChange={e => setBundleQtyToAdd(e.target.value)} />
              </div>
              <div className="md:col-span-2 flex items-end">
                <button type="button" className="btn-secondary px-3 py-2 rounded" onClick={addBundleItem}>افزودن به لیست</button>
              </div>
            </div>
            <div className="text-xs text-gray-600">فرمت ذخیره: هر خط «SKU,Qty»</div>
          </div>
          <div>
            <label className="label">نیازمندی‌ها (هر خط: SKU,Qty)</label>
            <textarea className="input min-h-[80px]" value={value.requiredItems} onChange={e => onChange({ ...value, requiredItems: e.target.value })} />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className="label">قیمت باندل</label>
              <input className="input" type="number" value={value.bundlePrice} onChange={e => onChange({ ...value, bundlePrice: e.target.value })} />
            </div>
            <div>
              <label className="label">ارز</label>
              <input className="input" value={value.currency} onChange={e => onChange({ ...value, currency: e.target.value })} />
            </div>
          </div>
        </div>
      )}

      {value.kind === 'buyXGetY' && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div>
            <label className="label">محصول/واریانت خرید</label>
            <SkuPicker
              label="انتخاب محصول/واریانت"
              value={value.buySkuId}
              onChange={(next) => onChange({ ...value, buySkuId: next.skuId })}
              showLabel={false}
            />
          </div>
          <div>
            <label className="label">تعداد خرید</label>
            <input className="input" type="number" value={value.buyQty} onChange={e => onChange({ ...value, buyQty: e.target.value })} />
          </div>
          <div>
            <label className="label">محصول/واریانت هدیه</label>
            <SkuPicker
              label="انتخاب محصول/واریانت"
              value={value.getSkuId}
              onChange={(next) => onChange({ ...value, getSkuId: next.skuId })}
              showLabel={false}
            />
          </div>
          <div>
            <label className="label">تعداد هدیه</label>
            <input className="input" type="number" value={value.getQty} onChange={e => onChange({ ...value, getQty: e.target.value })} />
          </div>
        </div>
      )}
    </div>
  )
}

export function buildBenefit(value: BenefitBuilderState): BenefitDefinition | null {
  switch (value.kind) {
    case 'percentOff':
      return { kind: 'percentOff', percent: toNumber(value.percent) }
    case 'amountOff':
      return { kind: 'amountOff', amount: toNumber(value.amount), currency: value.currency || null }
    case 'fixedPrice':
      return { kind: 'fixedPrice', price: toNumber(value.price), currency: value.currency || null }
    case 'cashbackPercent':
      return { kind: 'cashbackPercent', percent: toNumber(value.percent) }
    case 'cashbackAmount':
      return { kind: 'cashbackAmount', amount: toNumber(value.amount), currency: value.currency || null }
    case 'bundleFixedPrice':
      return {
        kind: 'bundleFixedPrice',
        requiredItems: parseRequiredItems(value.requiredItems),
        bundlePrice: toNumber(value.bundlePrice),
        currency: value.currency || null,
      }
    case 'buyXGetY':
      return {
        kind: 'buyXGetY',
        buySkuId: value.buySkuId.trim(),
        buyQty: toNumber(value.buyQty),
        getSkuId: value.getSkuId.trim(),
        getQty: toNumber(value.getQty),
      }
    default:
      return null
  }
}

function makeBenefitState(kind: BenefitBuilderState['kind']): BenefitBuilderState {
  switch (kind) {
    case 'amountOff':
      return { kind, amount: '', currency: 'IRR' }
    case 'fixedPrice':
      return { kind, price: '', currency: 'IRR' }
    case 'cashbackPercent':
      return { kind, percent: '' }
    case 'cashbackAmount':
      return { kind, amount: '', currency: 'IRR' }
    case 'bundleFixedPrice':
      return { kind, requiredItems: '', bundlePrice: '', currency: 'IRR' }
    case 'buyXGetY':
      return { kind, buySkuId: '', buyQty: '', getSkuId: '', getQty: '' }
    case 'percentOff':
    default:
      return { kind: 'percentOff', percent: '' }
  }
}

function toNumber(input: string) {
  const value = Number.parseFloat(input || '0')
  return Number.isFinite(value) ? value : 0
}

function parseRequiredItems(input: string) {
  const lines = input.split(/\r?\n/).map(l => l.trim()).filter(Boolean)
  return lines.map(line => {
    const [sku, qtyRaw] = line.split(/[,\t]/).map(p => p.trim())
    const qty = qtyRaw ? Number.parseInt(qtyRaw, 10) : 1
    return { skuId: sku, qtyRequired: Number.isFinite(qty) && qty > 0 ? qty : 1 }
  }).filter(r => r.skuId)
}
