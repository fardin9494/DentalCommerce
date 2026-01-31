import { useEffect, useMemo, useState, type Dispatch, type SetStateAction, type FormEvent } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { formatJalaliDate } from '../../../shared/utils/date'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { createQuote, getApiErrorMessage } from '../api'
import { useCampaigns, useCreateCampaign, useDeleteCampaign, useUpdateCampaign } from '../queries'
import type { BenefitDefinition, EligibilityDefinition, Guardrails, PromotionCampaign, StackingMode } from '../types'
import { SkuPicker } from '../components/SkuPicker'
import { formatSkuLabel } from '../catalogSkuLabels'
import { listCatalogCategoryLeaves } from '../catalogApi'
import type { CatalogCategoryLeaf } from '../catalogTypes'
import {
  BenefitBuilder,
  EligibilityBuilder,
  buildBenefit,
  buildEligibility,
  makeDefaultBenefit,
  makeDefaultEligibility,
  type BenefitBuilderState,
  type EligibilityBuilderState,
} from '../components/RuleBuilders'

type FilterState = {
  active: 'all' | 'active' | 'inactive'
  name: string
  stackingGroup: string
  exclusiveGroup: string
  benefitKind: 'all' | BenefitDefinition['kind']
  benefitMin: string
  benefitMax: string
  productSkuId: string
  activeAt: string
}

type FormState = {
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
}

type TemplateKind =
  | 'percentOffProduct'
  | 'amountOffProduct'
  | 'fixedPriceProduct'
  | 'cashbackPercentProduct'
  | 'cashbackAmountProduct'
  | 'bundleFixedPrice'
  | 'buyXGetY'

const modalUi = {
  label: 'mb-2 block text-sm font-medium text-slate-700',
  input:
    'w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20',
  inputMono:
    'w-full rounded-lg border border-slate-300 px-4 py-2.5 text-sm font-mono placeholder-slate-400 transition-colors focus:border-emerald-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/20',
  checkbox: 'h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-2 focus:ring-emerald-500/20',
  btnPrimary:
    'inline-flex items-center justify-center gap-2 rounded-lg bg-emerald-600 px-5 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50',
  btnSecondary:
    'rounded-lg px-4 py-2.5 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50',
  btnSecondarySm:
    'rounded-lg px-3 py-2 text-sm font-medium text-slate-600 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50',
  btnPillActive:
    'rounded-lg bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-50',
  btnPill:
    'rounded-lg px-3 py-1.5 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-50',
} as const

const stackingOptions: Array<{ value: StackingMode; label: string; help: string }> = [
  { value: 'BestOfEachGroup', label: 'بهترین هر گروه', help: 'از هر گروه فقط بهترین کمپین انتخاب می‌شود.' },
  { value: 'BestPrice', label: 'بهترین قیمت', help: 'کمپینی انتخاب می‌شود که بیشترین تخفیف را بدهد.' },
  { value: 'PriorityOnly', label: 'فقط اولویت', help: 'تنها کمپین با بالاترین اولویت اعمال می‌شود.' },
  { value: 'Cascading', label: 'آبشاری', help: 'کمپین‌ها بر اساس اولویت و قابلیت ترکیب اعمال می‌شوند.' },
]

const emptyForm = (): FormState => ({
  name: '',
  isActive: true,
  validFrom: '',
  validTo: '',
  priority: '0',
  stackingGroup: '',
  stackingMode: 'BestOfEachGroup',
  combinableWithOtherPromotions: true,
  combinableWithCoupons: true,
  exclusiveGroup: '',
  maxDiscountPercent: '',
  maxDiscountAmount: '',
})

export function CampaignsPage() {
  const [filters, setFilters] = useState<FilterState>({
    active: 'all',
    name: '',
    stackingGroup: '',
    exclusiveGroup: '',
    benefitKind: 'all',
    benefitMin: '',
    benefitMax: '',
    productSkuId: '',
    activeAt: '',
  })
  const isActiveParam = filters.active === 'all' ? undefined : filters.active === 'active'
  const { data, isLoading } = useCampaigns(isActiveParam)
  const create = useCreateCampaign()
  const del = useDeleteCampaign()
  const [editing, setEditing] = useState<PromotionCampaign | null>(null)
  const update = useUpdateCampaign(editing?.id || '')
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [template, setTemplate] = useState<TemplateKind>('percentOffProduct')
  const [form, setForm] = useState<FormState>(() => emptyForm())
  const [eligibilityState, setEligibilityState] = useState<EligibilityBuilderState>(() => makePercentOffProductEligibility())
  const [benefitState, setBenefitState] = useState<BenefitBuilderState>(() => ({ kind: 'percentOff', percent: '' }))

  const [catalogCategories, setCatalogCategories] = useState<CatalogCategoryLeaf[]>([])
  const categoryLabelById = useMemo(() => {
    const map = new Map<string, string>()
    for (const c of catalogCategories) map.set(c.id, c.name)
    return map
  }, [catalogCategories])

  useEffect(() => {
    let active = true
    const load = async () => {
      try {
        const cats = await listCatalogCategoryLeaves()
        if (!active) return
        setCatalogCategories(cats || [])
      } catch {
        // Non-blocking: if we can't resolve category names, we fall back to showing ids.
      }
    }
    void load()
    return () => {
      active = false
    }
  }, [])

  const filtered = useMemo(() => {
    if (!data) return []
    let list = [...data]
    if (filters.name.trim()) {
      const q = filters.name.trim().toLowerCase()
      list = list.filter(c => (c.name || '').toLowerCase().includes(q))
    }
    if (filters.stackingGroup) {
      list = list.filter(c => (c.stackingGroup || '').includes(filters.stackingGroup))
    }
    if (filters.exclusiveGroup) {
      list = list.filter(c => (c.exclusiveGroup || '').includes(filters.exclusiveGroup))
    }
    if (filters.benefitKind !== 'all') {
      list = list.filter(c => c.benefit?.kind === filters.benefitKind)
    }
    if (filters.benefitMin || filters.benefitMax) {
      const min = filters.benefitMin ? Number.parseFloat(filters.benefitMin) : NaN
      const max = filters.benefitMax ? Number.parseFloat(filters.benefitMax) : NaN
      list = list.filter(c => {
        const v = getBenefitNumericValue(c.benefit)
        if (v == null) return false
        if (Number.isFinite(min) && v < min) return false
        if (Number.isFinite(max) && v > max) return false
        return true
      })
    }
    if (filters.productSkuId.trim()) {
      const skuKey = filters.productSkuId.trim().toLowerCase()
      list = list.filter(c => extractProductSkusFromEligibility(c.eligibility).some(s => s.toLowerCase() === skuKey))
    }
    if (filters.activeAt) {
      const target = new Date(filters.activeAt)
      if (!Number.isNaN(target.getTime())) {
        list = list.filter(c => {
          const fromOk = !c.validFrom || new Date(c.validFrom) <= target
          const toOk = !c.validTo || new Date(c.validTo) >= target
          return fromOk && toOk
        })
      }
    }
    return list
  }, [data, filters])

  const stackingGroupSuggestions = useMemo(() => {
    const base = ['Product', 'Category', 'Seasonal', 'Bundle', 'Cashback', 'Discount', 'Brand']
    const set = new Set<string>(base)
    ;(data ?? []).forEach(c => {
      const group = (c.stackingGroup || '').trim()
      if (group) set.add(group)
    })
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'fa'))
  }, [data])

  const exclusiveGroupSuggestions = useMemo(() => {
    const set = new Set<string>()
    ;(data ?? []).forEach(c => {
      const group = (c.exclusiveGroup || '').trim()
      if (group) set.add(group)
    })
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'fa'))
  }, [data])

  const resetForm = () => {
    setEditing(null)
    setForm(emptyForm())
    applyTemplate('percentOffProduct', setTemplate, setForm, setEligibilityState, setBenefitState, true)
    setDrawerOpen(false)
  }

  const startEdit = (c: PromotionCampaign) => {
    setEditing(c)
    setTemplate(mapBenefitKindToTemplate(c.benefit?.kind))
    setForm({
      name: c.name,
      isActive: c.isActive,
      validFrom: c.validFrom ? toLocalDateTime(c.validFrom) : '',
      validTo: c.validTo ? toLocalDateTime(c.validTo) : '',
      priority: String(c.priority ?? 0),
      stackingGroup: c.stackingGroup ?? '',
      stackingMode: c.stackingMode,
      combinableWithOtherPromotions: c.combinableWithOtherPromotions,
      combinableWithCoupons: c.combinableWithCoupons,
      exclusiveGroup: c.exclusiveGroup ?? '',
      maxDiscountPercent: c.guardrails?.maxDiscountPercent?.toString() ?? '',
      maxDiscountAmount: c.guardrails?.maxDiscountAmount?.toString() ?? '',
    })
    setEligibilityState(mapEligibilityToState(c.eligibility))
    setBenefitState(mapBenefitToState(c.benefit))
    setDrawerOpen(true)
  }

  const startCreate = (kind: TemplateKind = 'percentOffProduct') => {
    setEditing(null)
    applyTemplate(kind, setTemplate, setForm, setEligibilityState, setBenefitState, true)
    setDrawerOpen(true)
  }

  const startClone = (c: PromotionCampaign) => {
    setEditing(null)
    setTemplate('percentOffProduct')
    setForm({
      name: `${c.name} (کپی)`,
      isActive: false,
      validFrom: '',
      validTo: '',
      priority: String(c.priority ?? 0),
      stackingGroup: c.stackingGroup ?? '',
      stackingMode: c.stackingMode,
      combinableWithOtherPromotions: c.combinableWithOtherPromotions,
      combinableWithCoupons: c.combinableWithCoupons,
      exclusiveGroup: c.exclusiveGroup ?? '',
      maxDiscountPercent: c.guardrails?.maxDiscountPercent?.toString() ?? '',
      maxDiscountAmount: c.guardrails?.maxDiscountAmount?.toString() ?? '',
    })
    setEligibilityState(mapEligibilityToState(c.eligibility))
    setBenefitState(mapBenefitToState(c.benefit))
    setDrawerOpen(true)
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!form.name.trim()) {
      swalToastError('نام کمپین الزامی است.')
      return
    }
    if (!form.stackingGroup.trim()) {
      swalToastError('گروه انباشت الزامی است.')
      return
    }

    const validationError = validateTemplateInputs(eligibilityState, benefitState)
    if (validationError) {
      swalToastError(validationError)
      return
    }

    const eligibility = buildEligibility(eligibilityState)
    const benefit = buildBenefit(benefitState)
    if (!eligibility || !benefit) return

    const guardrails = buildGuardrails(form.maxDiscountPercent, form.maxDiscountAmount)
    const payload = {
      name: form.name.trim(),
      isActive: form.isActive,
      validFrom: form.validFrom || null,
      validTo: form.validTo || null,
      priority: Number.parseInt(form.priority || '0', 10) || 0,
      stackingGroup: form.stackingGroup.trim(),
      stackingMode: form.stackingMode,
      combinableWithOtherPromotions: form.combinableWithOtherPromotions,
      combinableWithCoupons: form.combinableWithCoupons,
      exclusiveGroup: form.exclusiveGroup.trim() || null,
      guardrails,
      eligibility,
      benefit,
    }

    try {
      if (editing) {
        await update.mutateAsync(payload)
        swalToastSuccess('کمپین به‌روزرسانی شد.')
      } else {
        await create.mutateAsync(payload)
        swalToastSuccess('کمپین ایجاد شد.')
      }
      resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ذخیره کمپین.'))
    }
  }

  const handleDelete = async (id: string) => {
    const ok = await swalConfirm({
      title: 'حذف کمپین',
      text: 'آیا از حذف کمپین مطمئن هستید؟',
      icon: 'warning',
      confirmText: 'بله',
      cancelText: 'خیر',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      swalToastSuccess('کمپین حذف شد.')
      if (editing?.id === id) resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در حذف کمپین.'))
    }
  }

  const stackingHelp = stackingOptions.find(o => o.value === form.stackingMode)?.help

  return (
    <div className="space-y-4">
      <PageHeader
        title="کمپین‌ها"
        actions={(
          <button type="button" className="btn px-3 py-2 rounded" onClick={() => startCreate('percentOffProduct')}>
            ایجاد کمپین جدید
          </button>
        )}
      >
        لیست کمپین‌ها کامل نمایش داده می‌شود؛ ایجاد/ویرایش در یک پنل جداگانه انجام می‌شود.
      </PageHeader>

      <div className="card p-4 space-y-3">
        <h3 className="font-semibold">فیلترها</h3>
        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div>
            <label className="label">وضعیت</label>
            <select className="input" value={filters.active} onChange={e => setFilters(f => ({ ...f, active: e.target.value as FilterState['active'] }))}>
              <option value="all">همه</option>
              <option value="active">فعال</option>
              <option value="inactive">غیرفعال</option>
            </select>
          </div>
          <div>
            <label className="label">نام</label>
            <input className="input" value={filters.name} onChange={e => setFilters(f => ({ ...f, name: e.target.value }))} placeholder="جستجو در نام..." />
          </div>
          <div>
            <label className="label">گروه انباشت</label>
            <input className="input" value={filters.stackingGroup} onChange={e => setFilters(f => ({ ...f, stackingGroup: e.target.value }))} />
            <div className="text-xs text-gray-500 mt-1">فقط کمپین‌های همین گروه نمایش داده می‌شوند.</div>
          </div>
          <div>
            <label className="label">گروه انحصاری</label>
            <input className="input" value={filters.exclusiveGroup} onChange={e => setFilters(f => ({ ...f, exclusiveGroup: e.target.value }))} />
            <div className="text-xs text-gray-500 mt-1">کمپین‌های هم‌گروه انحصاری کنار هم فیلتر می‌شوند.</div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div>
            <label className="label">نوع مزیت</label>
            <select className="input" value={filters.benefitKind} onChange={e => setFilters(f => ({ ...f, benefitKind: e.target.value as any }))}>
              <option value="all">همه</option>
              <option value="percentOff">درصدی</option>
              <option value="amountOff">ریالی</option>
              <option value="fixedPrice">قیمت ثابت</option>
              <option value="cashbackPercent">کش‌بک درصدی</option>
              <option value="cashbackAmount">کش‌بک ریالی</option>
              <option value="bundleFixedPrice">باندل قیمت ثابت</option>
              <option value="buyXGetY">بخر/بگیر</option>
            </select>
          </div>
          <div>
            <label className="label">حداقل مقدار</label>
            <input className="input" type="number" value={filters.benefitMin} onChange={e => setFilters(f => ({ ...f, benefitMin: e.target.value }))} />
          </div>
          <div>
            <label className="label">حداکثر مقدار</label>
            <input className="input" type="number" value={filters.benefitMax} onChange={e => setFilters(f => ({ ...f, benefitMax: e.target.value }))} />
          </div>
          <div>
            <label className="label">کالا (در شرط محصول)</label>
            <SkuPicker
              label="انتخاب محصول/واریانت"
              value={filters.productSkuId}
              onChange={(next) => setFilters(f => ({ ...f, productSkuId: next.skuId }))}
              showLabel={false}
            />
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
          <div>
            <label className="label">تاریخ فعال</label>
            <JalaliDateTimePicker value={filters.activeAt} onChange={(v) => setFilters(f => ({ ...f, activeAt: v }))} clearable />
          </div>
          <div className="md:col-span-3 flex items-end justify-end gap-2">
            <button
              type="button"
              className="btn-secondary px-3 py-2 rounded"
              onClick={() => setFilters({ active: 'all', name: '', stackingGroup: '', exclusiveGroup: '', benefitKind: 'all', benefitMin: '', benefitMax: '', productSkuId: '', activeAt: '' })}
            >
              پاک کردن فیلترها
            </button>
          </div>
        </div>
      </div>

      <div className="card p-4">
        {isLoading ? <Spinner /> : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">نام</th>
                  <th className="p-2">وضعیت</th>
                  <th className="p-2">گروه</th>
                  <th className="p-2">اولویت</th>
                  <th className="p-2">شرط</th>
                  <th className="p-2">مزیت</th>
                  <th className="p-2">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length === 0 && (
                  <tr className="border-b last:border-0">
                    <td colSpan={7} className="p-3 text-sm text-gray-600">موردی یافت نشد.</td>
                  </tr>
                )}
                {filtered.map(c => (
                  <tr key={c.id} className="border-b last:border-0">
                    <td className="p-2 text-right">
                      <div className="font-medium">{c.name}</div>
                      <div className="text-xs text-gray-600 mt-1">
                        {c.validFrom || c.validTo
                          ? `${formatJalaliDate(c.validFrom)} تا ${formatJalaliDate(c.validTo)}`
                          : 'بدون بازه'}
                      </div>
                    </td>
                    <td className="p-2">{c.isActive ? 'فعال' : 'غیرفعال'}</td>
                    <td className="p-2">{c.stackingGroup ?? '-'}</td>
                    <td className="p-2">{c.priority}</td>
                    <td className="p-2 text-right">
                      <div className="text-xs text-gray-700">{summarizeEligibility(c.eligibility, categoryLabelById)}</div>
                    </td>
                    <td className="p-2 text-right">
                      <div className="text-xs text-gray-700">{summarizeBenefit(c.benefit)}</div>
                    </td>
                    <td className="p-2">
                      <div className="flex items-center justify-center gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => startEdit(c)}>ویرایش</button>
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => startClone(c)}>کپی</button>
                        <button className="btn-red px-3 py-1.5 rounded" onClick={() => handleDelete(c.id)}>حذف</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <CampaignDrawer
        open={drawerOpen}
        editing={editing}
        template={template}
        onTemplateChange={(t) => applyTemplate(t, setTemplate, setForm, setEligibilityState, setBenefitState, false)}
        form={form}
        setForm={setForm}
        eligibilityState={eligibilityState}
        setEligibilityState={setEligibilityState}
        benefitState={benefitState}
          setBenefitState={setBenefitState}
          allCampaigns={data ?? []}
          stackingOptions={stackingOptions}
          stackingHelp={stackingHelp}
          stackingGroupSuggestions={stackingGroupSuggestions}
          exclusiveGroupSuggestions={exclusiveGroupSuggestions}
          isSaving={create.isPending || update.isPending}
        onClose={() => setDrawerOpen(false)}
        onReset={resetForm}
        onSubmit={handleSubmit}
      />
    </div>
  )
}

function CampaignDrawer(props: {
  open: boolean
  editing: PromotionCampaign | null
  template: TemplateKind
  onTemplateChange: (t: TemplateKind) => void
  form: FormState
  setForm: Dispatch<SetStateAction<FormState>>
  eligibilityState: EligibilityBuilderState
  setEligibilityState: Dispatch<SetStateAction<EligibilityBuilderState>>
    benefitState: BenefitBuilderState
    setBenefitState: Dispatch<SetStateAction<BenefitBuilderState>>
    allCampaigns: PromotionCampaign[]
    stackingOptions: Array<{ value: StackingMode; label: string; help: string }>
    stackingHelp?: string
    stackingGroupSuggestions: string[]
    exclusiveGroupSuggestions: string[]
    isSaving: boolean
    onClose: () => void
    onReset: () => void
  onSubmit: (e: FormEvent) => void
}) {
  if (!props.open) return null

  const selectedSkus = tryGetProductSkuList(props.eligibilityState)
  const allowProductSkuList = selectedSkus.length > 0 || !props.editing
  const riskWarnings = buildRiskWarnings(props.allCampaigns, props.editing?.id ?? null, props.form, props.benefitState, props.eligibilityState)

  const [previewSiteId, setPreviewSiteId] = useState('')
  const [previewUserId, setPreviewUserId] = useState('')
  const [previewSku, setPreviewSku] = useState('')
  const [previewQty, setPreviewQty] = useState('1')
  const [previewLoading, setPreviewLoading] = useState(false)
  const [previewResult, setPreviewResult] = useState<any>(null)

  const [draftSiteId, setDraftSiteId] = useState('')
  const [draftUserId, setDraftUserId] = useState('')
  const [draftItems, setDraftItems] = useState<Array<{ skuId: string; qty: string }>>([])
  const [draftLoading, setDraftLoading] = useState(false)
  const [draftBaseQuote, setDraftBaseQuote] = useState<any>(null)
  const [draftEstimate, setDraftEstimate] = useState<any>(null)

  const addSku = (skuId: string) => {
    const sku = skuId.trim()
    if (!sku) return
    props.setEligibilityState(prev => {
      const next = ensureSingleProductEligibility(prev)
      const current = String((next.conditions?.[0] as any)?.list || '')
      const lines = current.split('\n').map(s => s.trim()).filter(Boolean)
      if (!lines.some(x => x.toLowerCase() === sku.toLowerCase())) lines.push(sku)
      ;(next.conditions[0] as any).list = lines.join('\n')
      return { ...next, conditions: [...next.conditions] }
    })
    if (!props.form.stackingGroup.trim()) {
      props.setForm(f => ({ ...f, stackingGroup: 'Product' }))
    }
  }

  const clearSkus = () => {
    props.setEligibilityState(makePercentOffProductEligibility())
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={props.onClose} />

      {/* Modal */}
      <div className="relative w-[96vw] max-w-4xl max-h-[90vh] rounded-2xl bg-white shadow-xl flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div>
            <h2 className="text-xl font-bold text-slate-900">{props.editing ? 'ویرایش کمپین' : 'ایجاد کمپین'}</h2>
            <p className="mt-1 text-xs text-slate-500">ایجاد/ویرایش کمپین با قالب‌های آماده</p>
          </div>
          <button
            type="button"
            onClick={props.onClose}
            className="rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600"
            title="بستن"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M6 18 18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <form className="flex-1 flex flex-col min-h-0" onSubmit={props.onSubmit}>
          <div className="flex-1 overflow-y-auto p-6 space-y-4 min-h-0">
            <TemplatePanel
              template={props.template}
              editing={!!props.editing}
              benefitState={props.benefitState}
              setBenefitState={props.setBenefitState}
              selectedSkus={selectedSkus}
              addSku={addSku}
              clearSkus={clearSkus}
              onTemplateChange={props.onTemplateChange}
              allowProductSkuList={allowProductSkuList}
            />

          {riskWarnings.length > 0 && (
            <div className="border rounded-md p-3 bg-amber-50 space-y-2">
              <div className="font-semibold text-sm text-amber-900">هشدارهای ریسک/تضاد</div>
              <ul className="list-disc pr-5 text-sm text-amber-900 space-y-1">
                {riskWarnings.map((w, idx) => (
                  <li key={idx}>{w}</li>
                ))}
              </ul>
            </div>
          )}

          <div className="border rounded-md p-3 space-y-3">
            <div className="font-semibold text-sm">اطلاعات عمومی</div>
            <div>
              <label className={modalUi.label}>نام</label>
              <input className={modalUi.input} value={props.form.name} onChange={e => props.setForm(f => ({ ...f, name: e.target.value }))} />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className={modalUi.label}>از</label>
                <JalaliDateTimePicker value={props.form.validFrom} onChange={(v) => props.setForm(f => ({ ...f, validFrom: v }))} clearable />
              </div>
              <div>
                <label className={modalUi.label}>تا</label>
                <JalaliDateTimePicker value={props.form.validTo} onChange={(v) => props.setForm(f => ({ ...f, validTo: v }))} clearable />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className={modalUi.label}>اولویت</label>
                <input className={modalUi.input} type="number" value={props.form.priority} onChange={e => props.setForm(f => ({ ...f, priority: e.target.value }))} />
              </div>
              <div>
                <label className={modalUi.label}>گروه انباشت (StackingGroup)</label>
                <input
                  className={modalUi.input}
                  value={props.form.stackingGroup}
                  onChange={e => props.setForm(f => ({ ...f, stackingGroup: e.target.value }))}
                  placeholder="مثلاً Product"
                  list="campaign-stacking-group-options"
                />
                <datalist id="campaign-stacking-group-options">
                  {props.stackingGroupSuggestions.map(group => (
                    <option key={group} value={group} />
                  ))}
                </datalist>
                {props.stackingGroupSuggestions.length > 0 && (
                  <div className="mt-2 flex flex-wrap gap-2 text-xs">
                    {props.stackingGroupSuggestions.map(group => (
                      <button
                        key={group}
                        type="button"
                        className="btn-secondary px-2 py-1 rounded"
                        onClick={() => props.setForm(f => ({ ...f, stackingGroup: group }))}
                      >
                        {group}
                      </button>
                    ))}
                    <button
                      type="button"
                      className="btn-secondary px-2 py-1 rounded"
                      onClick={() => props.setForm(f => ({ ...f, stackingGroup: '' }))}
                    >
                      خالی
                    </button>
                  </div>
                )}
                <div className="text-xs text-gray-500 mt-1">گروه‌بندی کمپین‌ها تعیین می‌کند در حالت «بهترین هر گروه»، کدام کمپین از هر گروه انتخاب شود.</div>
              </div>
            </div>
            <div>
              <label className={modalUi.label}>حالت انباشت</label>
              <select className={modalUi.input} value={props.form.stackingMode} onChange={e => props.setForm(f => ({ ...f, stackingMode: e.target.value as StackingMode }))}>
                {props.stackingOptions.map(o => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
              {props.stackingHelp ? <p className="text-xs text-gray-600 mt-1">{props.stackingHelp}</p> : null}
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input className={modalUi.checkbox} type="checkbox" checked={props.form.combinableWithOtherPromotions} onChange={e => props.setForm(f => ({ ...f, combinableWithOtherPromotions: e.target.checked }))} />
                قابل ترکیب با سایر کمپین‌ها
              </label>
              <label className="flex items-center gap-2 text-sm text-slate-700">
                <input className={modalUi.checkbox} type="checkbox" checked={props.form.combinableWithCoupons} onChange={e => props.setForm(f => ({ ...f, combinableWithCoupons: e.target.checked }))} />
                قابل ترکیب با کد تخفیف
              </label>
            </div>
            <div>
              <label className={modalUi.label}>گروه انحصاری (اختیاری)</label>
                <input
                  className={modalUi.input}
                  value={props.form.exclusiveGroup}
                  onChange={e => props.setForm(f => ({ ...f, exclusiveGroup: e.target.value }))}
                  list="campaign-exclusive-group-options"
                />
                <datalist id="campaign-exclusive-group-options">
                  {props.exclusiveGroupSuggestions.map(group => (
                    <option key={group} value={group} />
                  ))}
                </datalist>
                {props.exclusiveGroupSuggestions.length > 0 && (
                  <div className="mt-2 flex flex-wrap gap-2 text-xs">
                    {props.exclusiveGroupSuggestions.map(group => (
                      <button
                        key={group}
                        type="button"
                        className="btn-secondary px-2 py-1 rounded"
                        onClick={() => props.setForm(f => ({ ...f, exclusiveGroup: group }))}
                      >
                        {group}
                      </button>
                    ))}
                    <button
                      type="button"
                      className="btn-secondary px-2 py-1 rounded"
                      onClick={() => props.setForm(f => ({ ...f, exclusiveGroup: '' }))}
                    >
                      خالی
                    </button>
                  </div>
                )}
              <div className="text-xs text-gray-500 mt-1">اگر چند کمپین در یک گروه انحصاری باشند، فقط یکی از آنها اعمال می‌شود.</div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className={modalUi.label}>حداکثر درصد تخفیف</label>
                <input className={modalUi.input} type="number" value={props.form.maxDiscountPercent} onChange={e => props.setForm(f => ({ ...f, maxDiscountPercent: e.target.value }))} />
              </div>
              <div>
                <label className={modalUi.label}>حداکثر مبلغ تخفیف</label>
                <input className={modalUi.input} type="number" value={props.form.maxDiscountAmount} onChange={e => props.setForm(f => ({ ...f, maxDiscountAmount: e.target.value }))} />
              </div>
            </div>
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input className={modalUi.checkbox} type="checkbox" checked={props.form.isActive} onChange={e => props.setForm(f => ({ ...f, isActive: e.target.checked }))} />
              فعال باشد
            </label>
          </div>

          <details className="border rounded-md p-3 space-y-3">
            <summary className="cursor-pointer select-none font-semibold text-sm">تنظیمات پیشرفته (اختیاری)</summary>
            <div className="mt-3 space-y-3">
              <div className="border rounded-md p-3 space-y-2">
                <h4 className="font-semibold">شرایط (Eligibility)</h4>
                <EligibilityBuilder value={props.eligibilityState} onChange={props.setEligibilityState} />
              </div>
              <div className="border rounded-md p-3 space-y-2">
                <h4 className="font-semibold">مزیت (Benefit)</h4>
                <BenefitBuilder value={props.benefitState} onChange={props.setBenefitState} />
              </div>
            </div>
          </details>

          <details className="border rounded-md p-3 space-y-3">
            <summary className="cursor-pointer select-none font-semibold text-sm">پیش‌نمایش (بعد از ذخیره)</summary>
            <div className="mt-3 space-y-3">
              <div className="text-xs text-gray-600">
                برای مشاهده اثر واقعی در موتور قیمت‌گذاری، ابتدا کمپین را ذخیره کنید. این پیش‌نمایش خروجی کامل قیمت‌گذاری را نشان می‌دهد (ممکن است سایر قوانین هم اثر بگذارند).
              </div>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className={modalUi.label}>SiteId</label>
                  <input className={modalUi.input} value={previewSiteId} onChange={e => setPreviewSiteId(e.target.value)} placeholder="Guid سایت" />
                </div>
                <div>
                  <label className={modalUi.label}>UserId (اختیاری)</label>
                  <input className={modalUi.input} value={previewUserId} onChange={e => setPreviewUserId(e.target.value)} placeholder="Guid کاربر" />
                </div>
                <div>
                  <label className={modalUi.label}>محصول/واریانت</label>
                  <SkuPicker
                    label="انتخاب محصول/واریانت"
                    value={previewSku}
                    onChange={(next) => setPreviewSku(next.skuId)}
                    showLabel
                  />
                  {selectedSkus.length > 0 && (
                    <div className="mt-2 flex flex-wrap gap-2 text-xs">
                      {selectedSkus.slice(0, 4).map(s => (
                        <button key={s} type="button" className={modalUi.btnPill} onClick={() => setPreviewSku(s)}>
                          {formatSkuLabel(s) || s}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
                <div>
                  <label className={modalUi.label}>تعداد</label>
                  <input className={modalUi.input} type="number" value={previewQty} onChange={e => setPreviewQty(e.target.value)} min={1} />
                </div>
              </div>

              <button
                type="button"
                className={modalUi.btnSecondary}
                disabled={previewLoading}
                onClick={async () => {
                  if (!props.editing?.id) {
                    swalToastError('برای پیش‌نمایش، ابتدا کمپین را ذخیره کنید.')
                    return
                  }
                  if (!previewSiteId.trim()) {
                    swalToastError('SiteId را وارد کنید.')
                    return
                  }
                  if (!previewSku.trim()) {
                    swalToastError('یک محصول/واریانت برای پیش‌نمایش انتخاب کنید.')
                    return
                  }
                  const qty = Number.parseInt(previewQty || '1', 10)
                  if (!Number.isFinite(qty) || qty <= 0) {
                    swalToastError('تعداد معتبر نیست.')
                    return
                  }
                  setPreviewLoading(true)
                  setPreviewResult(null)
                  try {
                    const res = await createQuote({
                      siteId: previewSiteId.trim(),
                      userId: previewUserId.trim() || null,
                      couponCode: null,
                      timestamp: null,
                      items: [{ skuId: previewSku.trim(), qty, batchId: null }],
                    } as any)
                    setPreviewResult(res)
                  } catch (err) {
                    swalToastError(getApiErrorMessage(err, 'خطا در پیش‌نمایش.'))
                  } finally {
                    setPreviewLoading(false)
                  }
                }}
              >
                {previewLoading ? 'در حال محاسبه...' : 'پیش‌نمایش با Quote'}
              </button>

              {previewResult && (
                <div className="border rounded-md p-3 bg-gray-50 space-y-2 text-sm">
                  <div className="font-semibold">نتیجه</div>
                  <div>Subtotal: {previewResult.subtotal}</div>
                  <div>DiscountTotal: {previewResult.discountTotal}</div>
                  <div>FinalTotal: {previewResult.finalTotal}</div>
                  <div className="text-xs text-gray-700 mt-2">
                    آیا این کمپین اعمال شد؟{' '}
                    {Array.isArray(previewResult.appliedSources) && previewResult.appliedSources.some((s: any) => s.sourceType === 'promotion' && String(s.sourceId) === String(props.editing?.id))
                      ? 'بله'
                      : 'خیر'}
                  </div>
                </div>
              )}
            </div>
          </details>

          <details className="border rounded-md p-3 space-y-3">
            <summary className="cursor-pointer select-none font-semibold text-sm">پیش‌نمایش قبل از ذخیره (تقریبی)</summary>
            <div className="mt-3 space-y-3">
              <div className="text-xs text-gray-600">
                این پیش‌نمایش «تقریبی» است: ابتدا یک Quote پایه از سیستم می‌گیرد و سپس اثر کمپین فعلی را روی همان Quote شبیه‌سازی می‌کند.
                نتیجه نهایی ممکن است به علت کمپین‌های دیگر/استکینگ/گاردریل‌ها متفاوت باشد.
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                <div>
                  <label className={modalUi.label}>SiteId</label>
                  <input className={modalUi.input} value={draftSiteId} onChange={e => setDraftSiteId(e.target.value)} placeholder="Guid سایت" />
                </div>
                <div>
                  <label className={modalUi.label}>UserId (اختیاری)</label>
                  <input className={modalUi.input} value={draftUserId} onChange={e => setDraftUserId(e.target.value)} placeholder="Guid کاربر" />
                </div>
              </div>

              <div className="border rounded-md p-3 bg-gray-50 space-y-2">
                <div className="flex items-center justify-between gap-2">
                  <div className="font-semibold text-sm">سبد تست</div>
                  <button
                    type="button"
                    className={modalUi.btnPill}
                    onClick={() => setDraftItems(prev => [...prev, { skuId: '', qty: '1' }])}
                  >
                    افزودن آیتم
                  </button>
                </div>

                {draftItems.length === 0 ? (
                  <div className="text-sm text-gray-600">حداقل یک آیتم اضافه کنید.</div>
                ) : (
                  <div className="space-y-2">
                    {draftItems.map((it, idx) => (
                      <div key={idx} className="grid grid-cols-1 md:grid-cols-3 gap-2 items-end">
                        <div className="md:col-span-2">
                          <label className={modalUi.label}>محصول/واریانت</label>
                          <SkuPicker
                            label="انتخاب محصول/واریانت"
                            value={it.skuId}
                            onChange={(next) => setDraftItems(prev => prev.map((x, i) => i === idx ? { ...x, skuId: next.skuId } : x))}
                            showLabel
                          />
                        </div>
                        <div className="flex items-end gap-2">
                          <div className="flex-1">
                            <label className={modalUi.label}>تعداد</label>
                            <input
                              className={modalUi.input}
                              type="number"
                              min={1}
                              value={it.qty}
                              onChange={e => setDraftItems(prev => prev.map((x, i) => i === idx ? { ...x, qty: e.target.value } : x))}
                            />
                          </div>
                          <button
                            type="button"
                            className="rounded-lg bg-red-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-colors hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-50"
                            onClick={() => setDraftItems(prev => prev.filter((_, i) => i !== idx))}
                            title="حذف"
                          >
                            حذف
                          </button>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </div>

              <button
                type="button"
                className={modalUi.btnSecondary}
                disabled={draftLoading}
                onClick={async () => {
                  if (!draftSiteId.trim()) {
                    swalToastError('SiteId را وارد کنید.')
                    return
                  }
                  const items = draftItems
                    .map(x => ({ skuId: x.skuId.trim(), qty: Number.parseInt(x.qty || '1', 10) }))
                    .filter(x => x.skuId && Number.isFinite(x.qty) && x.qty > 0)
                  if (items.length === 0) {
                    swalToastError('حداقل یک آیتم معتبر اضافه کنید.')
                    return
                  }

                  const eligibilityDef = buildEligibility(props.eligibilityState)
                  const benefitDef = buildBenefit(props.benefitState)
                  if (!eligibilityDef || !benefitDef) return

                  setDraftLoading(true)
                  setDraftBaseQuote(null)
                  setDraftEstimate(null)
                  try {
                    const baseQuote = await createQuote({
                      siteId: draftSiteId.trim(),
                      userId: draftUserId.trim() || null,
                      couponCode: null,
                      timestamp: null,
                      items: items.map(i => ({ skuId: i.skuId, qty: i.qty, batchId: null })),
                    } as any)
                    setDraftBaseQuote(baseQuote)

                    const estimate = estimateCampaignImpact(baseQuote, eligibilityDef, benefitDef)
                    setDraftEstimate(estimate)
                  } catch (err) {
                    swalToastError(getApiErrorMessage(err, 'خطا در پیش‌نمایش تقریبی.'))
                  } finally {
                    setDraftLoading(false)
                  }
                }}
              >
                {draftLoading ? 'در حال محاسبه...' : 'محاسبه پیش‌نمایش تقریبی'}
              </button>

              {draftBaseQuote && (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div className="border rounded-md p-3 bg-gray-50 space-y-2 text-sm">
                    <div className="font-semibold">Quote پایه (بدون کمپین جدید)</div>
                    <div>Subtotal: {draftBaseQuote.subtotal}</div>
                    <div>DiscountTotal: {draftBaseQuote.discountTotal}</div>
                    <div>FinalTotal: {draftBaseQuote.finalTotal}</div>
                    <div>CashbackTotal: {draftBaseQuote.cashbackTotal}</div>
                  </div>
                  <div className="border rounded-md p-3 bg-gray-50 space-y-2 text-sm">
                    <div className="font-semibold">اثر تخمینی کمپین فعلی</div>
                    {draftEstimate?.note ? <div className="text-xs text-amber-700">{draftEstimate.note}</div> : null}
                    <div>Δ تخفیف: {draftEstimate?.deltaDiscount ?? 0}</div>
                    <div>FinalTotal تخمینی: {draftEstimate?.estimatedFinalTotal ?? draftBaseQuote.finalTotal}</div>
                    <div>CashbackTotal تخمینی: {draftEstimate?.estimatedCashbackTotal ?? draftBaseQuote.cashbackTotal}</div>
                  </div>
                </div>
              )}
            </div>
          </details>

          </div>

          <div className="flex items-center justify-end gap-3 border-t bg-white px-6 py-4">
            {props.editing && (
              <button type="button" className={modalUi.btnSecondary} onClick={props.onReset} disabled={props.isSaving}>
                انصراف
              </button>
            )}
            <button type="submit" className={modalUi.btnPrimary} disabled={props.isSaving}>
              {props.isSaving ? 'در حال ذخیره...' : (props.editing ? 'ذخیره تغییرات' : 'ایجاد کمپین')}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function makePercentOffProductEligibility(): EligibilityBuilderState {
  return {
    mode: 'single',
    conditions: [{ id: 1, kind: 'product', list: '', values: {} } as any],
  }
}

function ensureSingleProductEligibility(state: EligibilityBuilderState): EligibilityBuilderState {
  if (state.mode === 'single' && state.conditions?.length === 1 && (state.conditions[0] as any)?.kind === 'product') return state
  return makePercentOffProductEligibility()
}

function tryGetProductSkuList(state: EligibilityBuilderState) {
  if (!(state.mode === 'single' && state.conditions?.length === 1 && (state.conditions[0] as any)?.kind === 'product')) return []
  const list = String((state.conditions?.[0] as any)?.list || '')
  return list.split('\n').map(x => x.trim()).filter(Boolean)
}

function mapBenefitKindToTemplate(kind: string | undefined | null): TemplateKind {
  switch (kind) {
    case 'amountOff':
      return 'amountOffProduct'
    case 'fixedPrice':
      return 'fixedPriceProduct'
    case 'cashbackPercent':
      return 'cashbackPercentProduct'
    case 'cashbackAmount':
      return 'cashbackAmountProduct'
    case 'bundleFixedPrice':
      return 'bundleFixedPrice'
    case 'buyXGetY':
      return 'buyXGetY'
    case 'percentOff':
    default:
      return 'percentOffProduct'
  }
}

function summarizeEligibility(def: EligibilityDefinition, categoryLabelById?: Map<string, string>) {
  switch (def.kind) {
    case 'all':
      return 'بدون شرط (همه)'
    case 'product': {
      const items = (def.skuIds || []).slice(0, 3).map(s => formatSkuLabel(s) || s)
      return `محصول: ${items.join('، ')}${(def.skuIds || []).length > 3 ? '…' : ''}`
    }
    case 'category':
      return `دسته‌بندی: ${(def.categoryIds || []).slice(0, 2).map(id => categoryLabelById?.get(id) || id).join('، ')}${(def.categoryIds || []).length > 2 ? '…' : ''}`
    case 'brand':
      return `برند: ${(def.brandIds || []).slice(0, 2).join('، ')}${(def.brandIds || []).length > 2 ? '…' : ''}`
    case 'tag':
      return `برچسب: ${(def.tags || []).slice(0, 3).join('، ')}${(def.tags || []).length > 3 ? '…' : ''}`
    case 'site':
      return `سایت: ${(def.siteIds || []).slice(0, 2).join('، ')}${(def.siteIds || []).length > 2 ? '…' : ''}`
    case 'user':
      return `کاربر: ${(def.userIds || []).slice(0, 2).join('، ')}${(def.userIds || []).length > 2 ? '…' : ''}`
    case 'seasonal':
      return `فصلی: ${def.from || '—'} تا ${def.to || '—'}`
    case 'bundle':
      return `باندل: ${(def.requirements || []).slice(0, 2).map(r => `${formatSkuLabel(r.skuId) || r.skuId}×${r.qty}`).join('، ')}${(def.requirements || []).length > 2 ? '…' : ''}`
    case 'batchExpiryBefore':
      return `انقضای بچ: ${def.date ? def.date : def.withinDays ? `تا ${def.withinDays} روز` : '—'}`
    case 'allOf':
      return `همه شروط (${def.conditions.length})`
    case 'anyOf':
      return `هرکدام (${def.conditions.length})`
    default:
      return def.kind
  }
}

function summarizeBenefit(def: BenefitDefinition) {
  switch (def.kind) {
    case 'percentOff':
      return `تخفیف درصدی: ${def.percent}%`
    case 'amountOff':
      return `تخفیف ریالی: ${def.amount}`
    case 'fixedPrice':
      return `قیمت ثابت: ${def.price}`
    case 'cashbackPercent':
      return `کش‌بک درصدی: ${def.percent}%`
    case 'cashbackAmount':
      return `کش‌بک ریالی: ${def.amount}`
    case 'bundleFixedPrice':
      return `باندل قیمت ثابت: ${def.bundlePrice}`
    case 'buyXGetY':
      return `بخر ${def.buyQty} بگیر ${def.getQty}`
    default:
      return def.kind
  }
}

function buildRiskWarnings(
  campaigns: PromotionCampaign[],
  editingId: string | null,
  form: FormState,
  benefit: BenefitBuilderState,
  eligibility: EligibilityBuilderState,
) {
  const warnings: string[] = []

  const stackingGroup = (form.stackingGroup || '').trim()
  const exclusiveGroup = (form.exclusiveGroup || '').trim()

  if (exclusiveGroup) {
    warnings.push('گروه انحصاری تنظیم شده است؛ احتمالاً فقط یک کمپین از این گروه اعمال می‌شود و بقیه رد می‌شوند.')
  }

  if (!form.combinableWithOtherPromotions) {
    warnings.push('این کمپین «قابل ترکیب با سایر کمپین‌ها» نیست؛ ممکن است تخفیف‌های دیگر را مسدود کند.')
  }
  if (!form.combinableWithCoupons) {
    warnings.push('این کمپین «قابل ترکیب با کد تخفیف» نیست؛ ممکن است باعث رد شدن کوپن شود.')
  }
  if (form.stackingMode === 'Cascading' && !form.combinableWithOtherPromotions) {
    warnings.push('حالت «آبشاری» با «غیرقابل ترکیب» معمولاً رفتار غیرمنتظره ایجاد می‌کند؛ بهتر است یکی را بازبینی کنید.')
  }

  if (stackingGroup) {
    const sameGroupCount = campaigns.filter(c => c.id !== editingId && (c.stackingGroup || '').trim().toLowerCase() === stackingGroup.toLowerCase()).length
    if (sameGroupCount >= 5) {
      warnings.push(`در گروه انباشت «${stackingGroup}» تعداد ${sameGroupCount} کمپین دیگر وجود دارد؛ برای جلوگیری از شلوغی، گروه‌بندی را دقیق‌تر کنید.`)
    }
  }

  const pct =
    benefit.kind === 'percentOff' || benefit.kind === 'cashbackPercent'
      ? Number.parseFloat(benefit.percent || '')
      : NaN
  if (Number.isFinite(pct) && pct >= 50) {
    warnings.push('درصد بالا (۵۰٪ یا بیشتر) ممکن است به کف قیمت برخورد کند و باعث حذف کمپین/کوپن شود.')
  }

  const activeOverlaps = campaigns
    .filter(c => c.id !== editingId)
    .filter(c => c.isActive)
    .filter(c => overlapsDateRange(c.validFrom || null, c.validTo || null, form.validFrom || null, form.validTo || null))

  if (activeOverlaps.length >= 10) {
    warnings.push(`در بازه زمانی انتخاب‌شده حدود ${activeOverlaps.length} کمپین فعال دیگر هم وجود دارد؛ برای نتایج قابل پیش‌بینی، بازه/گروه/اولویت را دقیق تنظیم کنید.`)
  }

  const selectedSkus = tryGetProductSkuList(eligibility)
  if (selectedSkus.length > 10) {
    warnings.push('تعداد کالاهای انتخاب‌شده زیاد است؛ بهتر است برای مدیریت بهتر، کمپین را به چند کمپین کوچک‌تر تقسیم کنید.')
  }

  return warnings
}

function overlapsDateRange(aFrom: string | null, aTo: string | null, bFrom: string | null, bTo: string | null) {
  const aStart = aFrom ? new Date(aFrom).getTime() : Number.NEGATIVE_INFINITY
  const aEnd = aTo ? new Date(aTo).getTime() : Number.POSITIVE_INFINITY
  const bStart = bFrom ? new Date(bFrom).getTime() : Number.NEGATIVE_INFINITY
  const bEnd = bTo ? new Date(bTo).getTime() : Number.POSITIVE_INFINITY
  if (!Number.isFinite(aStart) && aFrom) return true
  if (!Number.isFinite(aEnd) && aTo) return true
  if (!Number.isFinite(bStart) && bFrom) return true
  if (!Number.isFinite(bEnd) && bTo) return true
  return aStart <= bEnd && bStart <= aEnd
}

function getBenefitNumericValue(benefit: BenefitDefinition) {
  if (!benefit) return null
  switch (benefit.kind) {
    case 'percentOff':
    case 'cashbackPercent':
      return typeof benefit.percent === 'number' ? benefit.percent : null
    case 'amountOff':
    case 'cashbackAmount':
      return typeof benefit.amount === 'number' ? benefit.amount : null
    case 'fixedPrice':
      return typeof benefit.price === 'number' ? benefit.price : null
    case 'bundleFixedPrice':
      return typeof benefit.bundlePrice === 'number' ? benefit.bundlePrice : null
    case 'buyXGetY':
      return typeof benefit.buyQty === 'number' ? benefit.buyQty : null
    default:
      return null
  }
}

function extractProductSkusFromEligibility(def: EligibilityDefinition): string[] {
  const out: string[] = []
  walkEligibility(def, out)
  return out
}

function walkEligibility(def: EligibilityDefinition, out: string[]) {
  if (!def || typeof def !== 'object') return
  if (def.kind === 'product') {
    ;(def.skuIds || []).forEach(s => out.push(s))
    return
  }
  if ((def.kind === 'allOf' || def.kind === 'anyOf') && Array.isArray(def.conditions)) {
    def.conditions.forEach(c => walkEligibility(c as any, out))
  }
}

function estimateCampaignImpact(baseQuote: any, eligibility: EligibilityDefinition, benefit: BenefitDefinition) {
  const result = {
    estimatedFinalTotal: baseQuote?.finalTotal ?? 0,
    estimatedCashbackTotal: baseQuote?.cashbackTotal ?? 0,
    deltaDiscount: 0,
    note: '',
  }

  if (!baseQuote || !Array.isArray(baseQuote.lines)) {
    result.note = 'Quote پایه نامعتبر است.'
    return result
  }

  if (eligibility.kind !== 'product') {
    result.note = 'این پیش‌نمایش تقریبی فقط برای شرط «محصول» دقیق‌تر است.'
  }

  const eligibleSkus = new Set<string>((eligibility.kind === 'product' ? eligibility.skuIds : []).map(s => String(s).toLowerCase()))

  const currency = String(baseQuote.currency || 'IRR')
  const round = (n: number) => (currency.toUpperCase() === 'IRR' || currency.toUpperCase() === 'IRT' ? Math.round(n) : Math.round(n * 100) / 100)

  const lines = baseQuote.lines.map((l: any) => ({ ...l }))
  let addedCashback = 0
  let addedDiscount = 0

  const getLine = (skuId: string) => lines.find((x: any) => String(x.skuId).toLowerCase() === String(skuId).toLowerCase())

  const applyPerLine = (line: any, nextUnitPrice: number, note?: string) => {
    const old = Number(line.finalUnitPrice)
    const next = round(Math.max(0, nextUnitPrice))
    line.finalUnitPrice = next
    const qty = Number(line.quantity) || 0
    addedDiscount += Math.max(0, (old - next) * qty)
    if (note && !result.note) result.note = note
  }

  if (benefit.kind === 'percentOff') {
    const pct = Number(benefit.percent)
    for (const line of lines) {
      if (line.isGift) continue
      if (!eligibleSkus.has(String(line.skuId).toLowerCase())) continue
      const old = Number(line.finalUnitPrice)
      applyPerLine(line, old * (1 - pct / 100))
    }
  } else if (benefit.kind === 'amountOff') {
    const amt = Number(benefit.amount)
    for (const line of lines) {
      if (line.isGift) continue
      if (!eligibleSkus.has(String(line.skuId).toLowerCase())) continue
      const old = Number(line.finalUnitPrice)
      applyPerLine(line, old - amt)
    }
  } else if (benefit.kind === 'fixedPrice') {
    const price = Number(benefit.price)
    for (const line of lines) {
      if (line.isGift) continue
      if (!eligibleSkus.has(String(line.skuId).toLowerCase())) continue
      const old = Number(line.finalUnitPrice)
      if (old <= price) continue
      applyPerLine(line, price)
    }
  } else if (benefit.kind === 'cashbackPercent') {
    const pct = Number(benefit.percent)
    for (const line of lines) {
      if (line.isGift) continue
      if (!eligibleSkus.has(String(line.skuId).toLowerCase())) continue
      const qty = Number(line.quantity) || 0
      addedCashback += Number(line.finalUnitPrice) * qty * pct / 100
    }
  } else if (benefit.kind === 'cashbackAmount') {
    const amt = Number(benefit.amount)
    for (const line of lines) {
      if (line.isGift) continue
      if (!eligibleSkus.has(String(line.skuId).toLowerCase())) continue
      const qty = Number(line.quantity) || 0
      addedCashback += amt * qty
    }
  } else if (benefit.kind === 'buyXGetY') {
    const buy = getLine(benefit.buySkuId)
    if (buy) {
      const buyQty = Number(buy.quantity) || 0
      const bundles = Math.floor(buyQty / Number(benefit.buyQty))
      const giftQty = bundles * Number(benefit.getQty)
      if (giftQty > 0) {
        result.note = `هدیه تخمینی: ${giftQty} عدد از ${formatSkuLabel(benefit.getSkuId) || benefit.getSkuId}`
      }
    }
  } else if (benefit.kind === 'bundleFixedPrice') {
    const req = Array.isArray(benefit.requiredItems) ? benefit.requiredItems : []
    if (req.length === 0) {
      result.note = 'برای باندل، آیتم‌های لازم مشخص نیست.'
    } else {
      const counts: number[] = []
      for (const r of req) {
        const line = getLine(r.skuId)
        if (!line) { counts.push(0); continue }
        const q = Number(line.quantity) || 0
        const need = Number(r.qtyRequired) || 1
        counts.push(need > 0 ? Math.floor(q / need) : 0)
      }
      const bundleCount = Math.min(...counts)
      if (bundleCount > 0) {
        const participating = req.map(r => {
          const line = getLine(r.skuId)!
          const partQty = bundleCount * Number(r.qtyRequired)
          return { line, partQty }
        })
        const total = participating.reduce((sum, x) => sum + Number(x.line.finalUnitPrice) * x.partQty, 0)
        const bundleTotal = Number(benefit.bundlePrice) * bundleCount
        if (total > bundleTotal && total > 0) {
          const discountTotal = total - bundleTotal
          for (const p of participating) {
            const share = (Number(p.line.finalUnitPrice) * p.partQty) / total
            const lineDiscountTotal = discountTotal * share
            const qty = Number(p.line.quantity) || 1
            const perUnitDiscount = lineDiscountTotal / qty
            applyPerLine(p.line, Number(p.line.finalUnitPrice) - perUnitDiscount)
          }
          result.note = `باندل تخمینی: ${bundleCount} بار`
        }
      } else {
        result.note = 'شرایط باندل در سبد فعلی کامل نیست.'
      }
    }
  } else {
    result.note = 'این نوع مزیت برای پیش‌نمایش تقریبی پشتیبانی نشده است.'
  }

  const baseFinalTotal = Number(baseQuote.finalTotal) || 0
  const estimatedFinalTotal = lines.filter((l: any) => !l.isGift).reduce((sum: number, l: any) => sum + Number(l.finalUnitPrice) * Number(l.quantity), 0)
  result.estimatedFinalTotal = round(estimatedFinalTotal)
  result.estimatedCashbackTotal = round((Number(baseQuote.cashbackTotal) || 0) + addedCashback)
  result.deltaDiscount = round(Math.max(0, baseFinalTotal - result.estimatedFinalTotal))

  return result
}

function applyTemplate(
  kind: TemplateKind,
  setTemplate: Dispatch<SetStateAction<TemplateKind>>,
  setForm: Dispatch<SetStateAction<FormState>>,
  setEligibilityState: Dispatch<SetStateAction<EligibilityBuilderState>>,
  setBenefitState: Dispatch<SetStateAction<BenefitBuilderState>>,
  resetFormFields: boolean,
) {
  setTemplate(kind)
  if (kind === 'bundleFixedPrice' || kind === 'buyXGetY') {
    setEligibilityState(makeDefaultEligibility())
  } else {
    setEligibilityState(makePercentOffProductEligibility())
  }

  setForm(prev => {
    const next = resetFormFields ? { ...emptyForm() } : { ...prev }
    if (kind === 'bundleFixedPrice' || kind === 'buyXGetY') next.stackingGroup = 'Bundle'
    else if (kind === 'cashbackPercentProduct' || kind === 'cashbackAmountProduct') next.stackingGroup = 'Cashback'
    else next.stackingGroup = 'Product'
    next.stackingMode = 'BestOfEachGroup'
    next.combinableWithOtherPromotions = true
    next.combinableWithCoupons = true
    return next
  })

  switch (kind) {
    case 'percentOffProduct':
      setBenefitState({ kind: 'percentOff', percent: '' })
      break
    case 'amountOffProduct':
      setBenefitState({ kind: 'amountOff', amount: '', currency: 'IRR' })
      break
    case 'fixedPriceProduct':
      setBenefitState({ kind: 'fixedPrice', price: '', currency: 'IRR' })
      break
    case 'cashbackPercentProduct':
      setBenefitState({ kind: 'cashbackPercent', percent: '' })
      break
    case 'cashbackAmountProduct':
      setBenefitState({ kind: 'cashbackAmount', amount: '', currency: 'IRR' })
      break
    case 'bundleFixedPrice':
      setBenefitState({ kind: 'bundleFixedPrice', requiredItems: '', bundlePrice: '', currency: 'IRR' })
      break
    case 'buyXGetY':
      setBenefitState({ kind: 'buyXGetY', buySkuId: '', buyQty: '1', getSkuId: '', getQty: '1' })
      break
  }
}

function validateTemplateInputs(eligibility: EligibilityBuilderState, benefit: BenefitBuilderState) {
  // Guard against broad mistakes for the product list shortcut (only when eligibility is product list)
  const selectedSkus = tryGetProductSkuList(eligibility)
  const isSingleProductEligibility =
    eligibility.mode === 'single' &&
    eligibility.conditions?.length === 1 &&
    (eligibility.conditions[0] as any)?.kind === 'product'

  if (isSingleProductEligibility && selectedSkus.length === 0) {
    return 'حداقل یک محصول/واریانت انتخاب کنید.'
  }

  if (benefit.kind === 'percentOff' || benefit.kind === 'cashbackPercent') {
    const pct = Number.parseFloat(benefit.percent || '')
    if (!Number.isFinite(pct) || pct <= 0 || pct > 100) return 'درصد باید بین 1 تا 100 باشد.'
  }
  if (benefit.kind === 'amountOff' || benefit.kind === 'cashbackAmount') {
    const amt = Number.parseFloat(benefit.amount || '')
    if (!Number.isFinite(amt) || amt <= 0) return 'مبلغ باید بزرگ‌تر از صفر باشد.'
  }
  if (benefit.kind === 'fixedPrice') {
    const price = Number.parseFloat(benefit.price || '')
    if (!Number.isFinite(price) || price < 0) return 'قیمت باید معتبر باشد.'
  }
  if (benefit.kind === 'bundleFixedPrice') {
    const price = Number.parseFloat(benefit.bundlePrice || '')
    if (!Number.isFinite(price) || price < 0) return 'قیمت باندل باید معتبر باشد.'
    const req = String(benefit.requiredItems || '').trim()
    if (!req) return 'برای باندل حداقل یک آیتم لازم است.'
  }
  if (benefit.kind === 'buyXGetY') {
    if (!benefit.buySkuId.trim() || !benefit.getSkuId.trim()) return 'برای بخر/بگیر، کالاها را مشخص کنید.'
    const bq = Number.parseInt(benefit.buyQty || '0', 10)
    const gq = Number.parseInt(benefit.getQty || '0', 10)
    if (!Number.isFinite(bq) || bq <= 0 || !Number.isFinite(gq) || gq <= 0) return 'مقادیر X و Y باید بزرگ‌تر از صفر باشند.'
  }

  return null
}

function TemplatePanel(props: {
  template: TemplateKind
  editing: boolean
  benefitState: BenefitBuilderState
  setBenefitState: Dispatch<SetStateAction<BenefitBuilderState>>
  selectedSkus: string[]
  addSku: (skuId: string) => void
  clearSkus: () => void
  onTemplateChange: (t: TemplateKind) => void
  allowProductSkuList: boolean
}) {
  const [bundleSkuToAdd, setBundleSkuToAdd] = useState('')
  const [bundleQtyToAdd, setBundleQtyToAdd] = useState('1')

  const templateOptions: Array<{ value: TemplateKind; label: string; hint: string }> = [
    { value: 'percentOffProduct', label: 'درصدی', hint: 'تخفیف درصدی روی محصول/واریانت' },
    { value: 'amountOffProduct', label: 'ریالی', hint: 'تخفیف مبلغی روی محصول/واریانت' },
    { value: 'fixedPriceProduct', label: 'قیمت ثابت', hint: 'قیمت ثابت برای محصول/واریانت' },
    { value: 'cashbackPercentProduct', label: 'کش‌بک درصدی', hint: 'کش‌بک درصدی (قیمت را کم نمی‌کند)' },
    { value: 'cashbackAmountProduct', label: 'کش‌بک ریالی', hint: 'کش‌بک مبلغی (قیمت را کم نمی‌کند)' },
    { value: 'bundleFixedPrice', label: 'باندل', hint: 'چند کالا با قیمت ثابت' },
    { value: 'buyXGetY', label: 'بخر/بگیر', hint: 'بخر X، بگیر Y (هدیه)' },
  ]

  return (
    <div className="border rounded-md p-3 bg-gray-50 space-y-3">
      <div className="flex items-center justify-between gap-2">
        <div className="font-semibold text-sm">قالب‌های آماده</div>
        <div className="text-xs text-gray-600">برای شروع سریع، قالب را انتخاب کنید.</div>
      </div>

      <div className="flex flex-wrap gap-2">
        {templateOptions.map(t => (
          <button
            key={t.value}
            type="button"
            className={props.template === t.value ? modalUi.btnPillActive : modalUi.btnPill}
            disabled={props.editing}
            onClick={() => props.onTemplateChange(t.value)}
            title={props.editing ? 'در حالت ویرایش، تغییر قالب توصیه نمی‌شود.' : t.hint}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        {props.benefitState.kind === 'percentOff' && (
          <div>
            <label className={modalUi.label}>درصد تخفیف</label>
            <input
              className={modalUi.input}
              type="number"
              value={props.benefitState.percent}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, percent: e.target.value })}
              placeholder="مثلاً 10"
            />
          </div>
        )}

        {props.benefitState.kind === 'amountOff' && (
          <div>
            <label className={modalUi.label}>مبلغ تخفیف (ریال)</label>
            <input
              className={modalUi.input}
              type="number"
              value={props.benefitState.amount}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, amount: e.target.value })}
              placeholder="مثلاً 50000"
            />
          </div>
        )}

        {props.benefitState.kind === 'fixedPrice' && (
          <div>
            <label className={modalUi.label}>قیمت ثابت (ریال)</label>
            <input
              className={modalUi.input}
              type="number"
              value={props.benefitState.price}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, price: e.target.value })}
              placeholder="مثلاً 120000"
            />
          </div>
        )}

        {props.benefitState.kind === 'cashbackPercent' && (
          <div>
            <label className={modalUi.label}>درصد کش‌بک</label>
            <input
              className={modalUi.input}
              type="number"
              value={props.benefitState.percent}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, percent: e.target.value })}
              placeholder="مثلاً 5"
            />
          </div>
        )}

        {props.benefitState.kind === 'cashbackAmount' && (
          <div>
            <label className={modalUi.label}>مبلغ کش‌بک (ریال)</label>
            <input
              className={modalUi.input}
              type="number"
              value={props.benefitState.amount}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, amount: e.target.value })}
              placeholder="مثلاً 20000"
            />
          </div>
        )}

        {props.allowProductSkuList && props.template.endsWith('Product') && (
          <div>
            <label className={modalUi.label}>افزودن محصول/واریانت</label>
            <SkuPicker label="افزودن" value="" onChange={(next) => props.addSku(next.skuId)} showLabel />
          </div>
        )}
      </div>

      {props.benefitState.kind === 'bundleFixedPrice' && (
        <div className="border rounded-md p-3 bg-white space-y-3">
          <div className="font-semibold text-sm">تنظیم باندل</div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className={modalUi.label}>قیمت باندل (ریال)</label>
              <input
                className={modalUi.input}
                type="number"
                value={props.benefitState.bundlePrice}
                onChange={(e) => props.setBenefitState({ ...props.benefitState, bundlePrice: e.target.value })}
              />
            </div>
            <div className="flex items-end gap-2">
              <div className="flex-1">
                <label className={modalUi.label}>افزودن کالا</label>
                <SkuPicker
                  label="افزودن کالا"
                  value={bundleSkuToAdd}
                  onChange={(next) => setBundleSkuToAdd(next.skuId)}
                  showLabel
                />
              </div>
              <div className="w-28">
                <label className={modalUi.label}>تعداد</label>
                <input className={modalUi.input} type="number" value={bundleQtyToAdd} onChange={(e) => setBundleQtyToAdd(e.target.value)} />
              </div>
              <button
                type="button"
                className={modalUi.btnSecondarySm}
                onClick={() => {
                  const sku = bundleSkuToAdd.trim()
                  if (!sku) return
                  const qty = Number.parseInt(bundleQtyToAdd || '1', 10)
                  const safeQty = Number.isFinite(qty) && qty > 0 ? qty : 1
                  const current = (props.benefitState.requiredItems || '').trim()
                  const nextLine = `${sku},${safeQty}`
                  const nextText = current ? `${current}\n${nextLine}` : nextLine
                  props.setBenefitState({ ...props.benefitState, requiredItems: nextText })
                  setBundleSkuToAdd('')
                  setBundleQtyToAdd('1')
                }}
              >
                افزودن
              </button>
            </div>
          </div>
          <div>
            <label className={modalUi.label}>آیتم‌های باندل (هر خط: SKU,Qty)</label>
            <textarea
              className={`${modalUi.input} min-h-[90px]`}
              value={props.benefitState.requiredItems}
              onChange={(e) => props.setBenefitState({ ...props.benefitState, requiredItems: e.target.value })}
              placeholder="SKU1,2\nSKU2,1"
            />
          </div>
        </div>
      )}

      {props.benefitState.kind === 'buyXGetY' && (
        <div className="border rounded-md p-3 bg-white space-y-3">
          <div className="font-semibold text-sm">تنظیم بخر/بگیر</div>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div>
              <label className={modalUi.label}>کالای خریدنی</label>
              <SkuPicker
                label="انتخاب کالای خریدنی"
                value={props.benefitState.buySkuId}
                onChange={(next) => props.setBenefitState({ ...props.benefitState, buySkuId: next.skuId })}
                showLabel
              />
            </div>
            <div>
              <label className={modalUi.label}>تعداد خرید (X)</label>
              <input className={modalUi.input} type="number" value={props.benefitState.buyQty} onChange={(e) => props.setBenefitState({ ...props.benefitState, buyQty: e.target.value })} />
            </div>
            <div>
              <label className={modalUi.label}>کالای هدیه</label>
              <SkuPicker
                label="انتخاب کالای هدیه"
                value={props.benefitState.getSkuId}
                onChange={(next) => props.setBenefitState({ ...props.benefitState, getSkuId: next.skuId })}
                showLabel
              />
            </div>
            <div>
              <label className={modalUi.label}>تعداد هدیه (Y)</label>
              <input className={modalUi.input} type="number" value={props.benefitState.getQty} onChange={(e) => props.setBenefitState({ ...props.benefitState, getQty: e.target.value })} />
            </div>
          </div>
        </div>
      )}

      {props.selectedSkus.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {props.selectedSkus.map(sku => (
            <span key={sku} className="badge badge-gray">
              {formatSkuLabel(sku) || sku}
            </span>
          ))}
          <button type="button" className={modalUi.btnPill} onClick={props.clearSkus}>
            پاک کردن لیست
          </button>
        </div>
      )}
    </div>
  )
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

function mapEligibilityToState(def: EligibilityDefinition): EligibilityBuilderState {
  if (def.kind === 'allOf' || def.kind === 'anyOf') {
    return {
      mode: def.kind === 'allOf' ? 'allOf' : 'anyOf',
      conditions: def.conditions.map((c, idx) => mapCondition(c, idx)),
    }
  }
  return {
    mode: 'single',
    conditions: [mapCondition(def, 0)],
  }
}

function mapCondition(def: EligibilityDefinition, idx: number) {
  switch (def.kind) {
    case 'product':
      return { id: idx + 1, kind: 'product', list: def.skuIds?.join('\n') ?? '', values: {} }
    case 'category':
      return { id: idx + 1, kind: 'category', list: def.categoryIds?.join('\n') ?? '', values: {} }
    case 'brand':
      return { id: idx + 1, kind: 'brand', list: def.brandIds?.join('\n') ?? '', values: {} }
    case 'tag':
      return { id: idx + 1, kind: 'tag', list: def.tags?.join('\n') ?? '', values: {} }
    case 'site':
      return { id: idx + 1, kind: 'site', list: def.siteIds?.join('\n') ?? '', values: {} }
    case 'user':
      return { id: idx + 1, kind: 'user', list: def.userIds?.join('\n') ?? '', values: {} }
    case 'seasonal':
      return { id: idx + 1, kind: 'seasonal', values: { from: def.from || '', to: def.to || '' } }
    case 'bundle':
      return {
        id: idx + 1,
        kind: 'bundle',
        list: def.requirements?.map(r => `${r.skuId},${r.qty}`).join('\n') ?? '',
        values: {},
      }
    case 'batchExpiryBefore':
      return {
        id: idx + 1,
        kind: 'batchExpiryBefore',
        values: { date: def.date || '', withinDays: def.withinDays?.toString() ?? '' },
      }
    default:
      return { id: idx + 1, kind: 'all', values: {} }
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

function toLocalDateTime(value: string) {
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}
