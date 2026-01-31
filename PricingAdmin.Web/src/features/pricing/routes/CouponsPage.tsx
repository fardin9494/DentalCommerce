import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { formatJalaliDate } from '../../../shared/utils/date'
import { swalConfirm, swalToastError, swalToastSuccess } from '../../../shared/utils/swal'
import { JalaliDateTimePicker } from '../../../shared/components/JalaliDateTimePicker'
import { createQuote, getApiErrorMessage } from '../api'
import { listCatalogStores } from '../catalogApi'
import type { CatalogStoreListItem } from '../catalogTypes'
import { useCouponReservations, useCouponUsage, useCreateCoupon, useCoupons, useDeleteCoupon, useUpdateCoupon } from '../queries'
import type { BenefitDefinition, Coupon, EligibilityDefinition, Guardrails, QuoteResponse } from '../types'
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
import { SkuPicker } from '../components/SkuPicker'
import { RejectedSourcesPanel } from '../components/RejectedSourcesPanel'

type FilterState = {
  active: 'all' | 'active' | 'inactive'
  code: string
}

type FormState = {
  code: string
  name: string
  isActive: boolean
  validFrom: string
  validTo: string
  priority: string
  maxUsesTotal: string
  maxUsesPerUser: string
  combinableWithPromotions: boolean
  exclusiveGroup: string
  maxDiscountPercent: string
  maxDiscountAmount: string
}

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
} as const

const emptyForm = (): FormState => ({
  code: '',
  name: '',
  isActive: true,
  validFrom: '',
  validTo: '',
  priority: '0',
  maxUsesTotal: '',
  maxUsesPerUser: '',
  combinableWithPromotions: true,
  exclusiveGroup: '',
  maxDiscountPercent: '',
  maxDiscountAmount: '',
})

export function CouponsPage() {
  const [filters, setFilters] = useState<FilterState>({ active: 'all', code: '' })
  const { data, isLoading } = useCoupons({
    isActive: filters.active === 'all' ? undefined : filters.active === 'active',
    code: filters.code || undefined,
  })
  const create = useCreateCoupon()
  const del = useDeleteCoupon()
  const [editing, setEditing] = useState<Coupon | null>(null)
  const update = useUpdateCoupon(editing?.id || '')
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [form, setForm] = useState<FormState>(() => emptyForm())
  const [eligibilityState, setEligibilityState] = useState<EligibilityBuilderState>(() => makeDefaultEligibility())
  const [benefitState, setBenefitState] = useState<BenefitBuilderState>(() => makeDefaultBenefit())
  const [catalogStores, setCatalogStores] = useState<CatalogStoreListItem[]>([])
  const [catalogStoresLoading, setCatalogStoresLoading] = useState(false)

  const filtered = useMemo(() => data ?? [], [data])
  const exclusiveGroupSuggestions = useMemo(() => {
    const set = new Set<string>()
    ;(data ?? []).forEach(c => {
      const group = (c.exclusiveGroup || '').trim()
      if (group) set.add(group)
    })
    return Array.from(set).sort((a, b) => a.localeCompare(b, 'fa'))
  }, [data])

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

  const resetForm = () => {
    setEditing(null)
    setForm(emptyForm())
    setEligibilityState(makeDefaultEligibility())
    setBenefitState(makeDefaultBenefit())
    setDrawerOpen(false)
  }

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm())
    setEligibilityState(makeDefaultEligibility())
    setBenefitState(makeDefaultBenefit())
    setDrawerOpen(true)
  }

  const startEdit = (c: Coupon) => {
    setEditing(c)
    setForm({
      code: c.code,
      name: c.name ?? '',
      isActive: c.isActive,
      validFrom: c.validFrom ? toLocalDateTime(c.validFrom) : '',
      validTo: c.validTo ? toLocalDateTime(c.validTo) : '',
      priority: String(c.priority ?? 0),
      maxUsesTotal: c.maxUsesTotal?.toString() ?? '',
      maxUsesPerUser: c.maxUsesPerUser?.toString() ?? '',
      combinableWithPromotions: c.combinableWithPromotions,
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
    const code = form.code.trim().toUpperCase()
    if (!code) {
      swalToastError('کد کوپن ضروری است.')
      return
    }
    if (/\s/.test(code)) {
      swalToastError('کد کوپن نباید فاصله داشته باشد.')
      return
    }
    if (form.validFrom && form.validTo) {
      const from = new Date(form.validFrom)
      const to = new Date(form.validTo)
      if (!Number.isNaN(from.getTime()) && !Number.isNaN(to.getTime()) && from > to) {
        swalToastError('تاریخ شروع نباید بعد از تاریخ پایان باشد.')
        return
      }
    }
    if (form.maxUsesTotal && Number.parseInt(form.maxUsesTotal, 10) < 0) {
      swalToastError('حداکثر استفاده کل باید عدد مثبت باشد.')
      return
    }
    if (form.maxUsesPerUser && Number.parseInt(form.maxUsesPerUser, 10) < 0) {
      swalToastError('حداکثر استفاده هر کاربر باید عدد مثبت باشد.')
      return
    }
    const eligibility = buildEligibility(eligibilityState)
    const benefit = buildBenefit(benefitState)
    if (!eligibility || !benefit) return

    const guardrails = buildGuardrails(form.maxDiscountPercent, form.maxDiscountAmount)
    const payload = {
      code,
      name: form.name.trim() || null,
      isActive: form.isActive,
      validFrom: form.validFrom || null,
      validTo: form.validTo || null,
      maxUsesTotal: form.maxUsesTotal ? Number.parseInt(form.maxUsesTotal, 10) : null,
      maxUsesPerUser: form.maxUsesPerUser ? Number.parseInt(form.maxUsesPerUser, 10) : null,
      priority: Number.parseInt(form.priority || '0', 10) || 0,
      combinableWithPromotions: form.combinableWithPromotions,
      exclusiveGroup: form.exclusiveGroup.trim() || null,
      guardrails,
      eligibility,
      benefit,
    }

    try {
      if (editing) {
        await update.mutateAsync(payload)
        swalToastSuccess('کوپن تخفیف بروزرسانی شد.')
      } else {
        await create.mutateAsync(payload)
        swalToastSuccess('کوپن تخفیف ایجاد شد.')
      }
      resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در ذخیره کد تخفیف.'))
    }
  }

  const handleDelete = async (id: string) => {
    const ok = await swalConfirm({
      title: 'حذف کوپن تخفیف',
      text: 'آیا از حذف کوپن تخفیف اطمینان دارید؟',
      icon: 'warning',
      confirmText: 'بله',
      cancelText: 'خیر',
    })
    if (!ok) return
    try {
      await del.mutateAsync(id)
      swalToastSuccess('کوپن تخفیف حذف شد.')
      if (editing?.id === id) resetForm()
    } catch (err) {
      swalToastError(getApiErrorMessage(err, 'خطا در حذف کد تخفیف.'))
    }
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="کدهای تخفیف"
        actions={(
          <button className="btn" onClick={openCreate}>
            ایجاد کوپن جدید
          </button>
        )}
      >
        مدیریت کوپن های تخفیف و محدودیت های استفاده از آن ها.
      </PageHeader>

      <div className="card p-4 space-y-3">
        <h3 className="font-semibold">فیلترها</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label className="label">وضعیت</label>
            <select className="input" value={filters.active} onChange={e => setFilters(f => ({ ...f, active: e.target.value as FilterState['active'] }))}>
              <option value="all">همه</option>
              <option value="active">فعال</option>
              <option value="inactive">غیرفعال</option>
            </select>
          </div>
          <div>
            <label className="label">کد</label>
            <input className="input" value={filters.code} onChange={e => setFilters(f => ({ ...f, code: e.target.value }))} />
          </div>
        </div>
      </div>

      <div className="card p-4">
        {isLoading ? <Spinner /> : (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">کد</th>
                  <th className="p-2">وضعیت</th>
                  <th className="p-2">بازه</th>
                  <th className="p-2">مزیت</th>
                  <th className="p-2">حداکثر استفاده</th>
                  <th className="p-2">عملیات</th>
                </tr>
              </thead>
              <tbody>
                {filtered.length === 0 && (
                  <tr className="border-b last:border-0">
                    <td colSpan={6} className="p-3 text-sm text-gray-600">موردی یافت نشد.</td>
                  </tr>
                )}
                {filtered.map(c => {
                  const activeNow = isActiveNow(c)
                  return (
                    <>
                      <tr key={c.id} className="border-b last:border-0">
                        <td className="p-2 font-mono">{c.code}</td>
                        <td className="p-2">
                          <span className={`badge ${activeNow ? 'badge-green' : 'badge-gray'}`}>
                            {activeNow ? 'فعال' : 'غیرفعال'}
                          </span>
                        </td>
                        <td className="p-2 text-xs">
                          <div>{formatRange(c.validFrom, c.validTo)}</div>
                        </td>
                        <td className="p-2 text-xs">{benefitLabel(c.benefit)}</td>
                        <td className="p-2">{c.maxUsesTotal ?? '-'} / {c.maxUsesPerUser ?? '-'}</td>
                        <td className="p-2">
                          <div className="flex items-center justify-center gap-2">
                            <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => setExpandedId(expandedId === c.id ? null : c.id)}>
                              {expandedId === c.id ? 'بستن' : 'جزئیات'}
                            </button>
                            <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => startEdit(c)}>ویرایش</button>
                            <button className="btn-red px-3 py-1.5 rounded" onClick={() => handleDelete(c.id)}>حذف</button>
                          </div>
                        </td>
                      </tr>
                      {expandedId === c.id && (
                        <tr className="border-b last:border-0 bg-gray-50">
                          <td colSpan={6} className="p-3 text-right">
                            <CouponDetails
                              coupon={c}
                              catalogStores={catalogStores}
                              catalogStoresLoading={catalogStoresLoading}
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
        )}
      </div>

      {drawerOpen && (
        <CouponModal
          editing={editing}
          form={form}
          setForm={setForm}
          eligibilityState={eligibilityState}
          setEligibilityState={setEligibilityState}
          benefitState={benefitState}
          setBenefitState={setBenefitState}
          onClose={resetForm}
          onSubmit={handleSubmit}
          isSaving={create.isPending || update.isPending}
          exclusiveGroupSuggestions={exclusiveGroupSuggestions}
        />
      )}
    </div>
  )
}

function CouponModal(props: {
  editing: Coupon | null
  form: FormState
  setForm: (next: FormState | ((prev: FormState) => FormState)) => void
  eligibilityState: EligibilityBuilderState
  setEligibilityState: (next: EligibilityBuilderState) => void
  benefitState: BenefitBuilderState
  setBenefitState: (next: BenefitBuilderState) => void
  onClose: () => void
  onSubmit: (e: FormEvent) => void
  isSaving: boolean
  exclusiveGroupSuggestions: string[]
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="absolute inset-0 bg-black/50 backdrop-blur-sm" onClick={props.onClose} />
      <div className="relative w-[96vw] max-w-4xl max-h-[90vh] rounded-2xl bg-white shadow-xl flex flex-col overflow-hidden">
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div>
            <div className="text-lg font-semibold">{props.editing ? 'ویرایش کوپن تخفیف' : 'ایجاد کوپن جدید'}</div>
            <div className="text-xs text-gray-500">جزئیات ساختاری شرایط و مزایای کوپن را مشخص کنید.</div>
          </div>
          <button className={modalUi.btnSecondary} onClick={props.onClose}>بستن</button>
        </div>
        <form className="flex-1 flex flex-col min-h-0" onSubmit={props.onSubmit}>
          <div className="flex-1 overflow-y-auto p-6 space-y-4 min-h-0">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className={modalUi.label}>کد</label>
              <input
                className={modalUi.inputMono}
                value={props.form.code}
                onChange={e => props.setForm(f => ({ ...f, code: e.target.value.toUpperCase() }))}
                placeholder="مثال: WELCOME10"
              />
            </div>
            <div>
              <label className={modalUi.label}>نام (اختیاری)</label>
              <input className={modalUi.input} value={props.form.name} onChange={e => props.setForm(f => ({ ...f, name: e.target.value }))} />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className={modalUi.label}>شروع</label>
              <JalaliDateTimePicker value={props.form.validFrom} onChange={(v) => props.setForm(f => ({ ...f, validFrom: v }))} clearable />
            </div>
            <div>
              <label className={modalUi.label}>پایان</label>
              <JalaliDateTimePicker value={props.form.validTo} onChange={(v) => props.setForm(f => ({ ...f, validTo: v }))} clearable />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className={modalUi.label}>اولویت</label>
              <input className={modalUi.input} type="number" value={props.form.priority} onChange={e => props.setForm(f => ({ ...f, priority: e.target.value }))} />
            </div>
            <div>
              <label className={modalUi.label}>گروه انحصاری (اختیاری)</label>
              <input
                className={modalUi.input}
                value={props.form.exclusiveGroup}
                onChange={e => props.setForm(f => ({ ...f, exclusiveGroup: e.target.value }))}
                list="coupon-exclusive-group-options"
              />
              <datalist id="coupon-exclusive-group-options">
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
              <div className="text-xs text-gray-500 mt-1">اگر چند کوپن در یک گروه انحصاری باشند، فقط یکی از آنها (طبق اولویت/قواعد) اعمال می‌شود.</div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className={modalUi.label}>حداکثر استفاده کل</label>
              <input className={modalUi.input} type="number" value={props.form.maxUsesTotal} onChange={e => props.setForm(f => ({ ...f, maxUsesTotal: e.target.value }))} />
            </div>
            <div>
              <label className={modalUi.label}>حداکثر استفاده هر کاربر</label>
              <input className={modalUi.input} type="number" value={props.form.maxUsesPerUser} onChange={e => props.setForm(f => ({ ...f, maxUsesPerUser: e.target.value }))} />
              <div className="text-xs text-gray-500 mt-1">برای اعمال محدودیت، ارائه UserId در Quote الزامی است.</div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className={modalUi.label}>حداکثر درصد تخفیف</label>
              <input className={modalUi.input} type="number" value={props.form.maxDiscountPercent} onChange={e => props.setForm(f => ({ ...f, maxDiscountPercent: e.target.value }))} />
            </div>
            <div>
              <label className={modalUi.label}>حداکثر مبلغ تخفیف</label>
              <input className={modalUi.input} type="number" value={props.form.maxDiscountAmount} onChange={e => props.setForm(f => ({ ...f, maxDiscountAmount: e.target.value }))} />
            </div>
          </div>

          <label className="flex items-center gap-2 text-sm">
            <input className={modalUi.checkbox} type="checkbox" checked={props.form.combinableWithPromotions} onChange={e => props.setForm(f => ({ ...f, combinableWithPromotions: e.target.checked }))} />
            قابل ترکیب با کمپین ها
          </label>
          <label className="flex items-center gap-2 text-sm">
            <input className={modalUi.checkbox} type="checkbox" checked={props.form.isActive} onChange={e => props.setForm(f => ({ ...f, isActive: e.target.checked }))} />
            فعال باشد
          </label>

          <div className="border rounded-md p-3 space-y-2">
            <h4 className="font-semibold">شرایط (Eligibility)</h4>
            <EligibilityBuilder value={props.eligibilityState} onChange={props.setEligibilityState} />
          </div>

          <div className="border rounded-md p-3 space-y-2">
            <h4 className="font-semibold">مزیت (Benefit)</h4>
            <BenefitBuilder value={props.benefitState} onChange={props.setBenefitState} />
          </div>

          </div>

          <div className="flex items-center gap-2 justify-end border-t bg-white px-6 py-4">
            <button type="button" className={modalUi.btnSecondary} onClick={props.onClose} disabled={props.isSaving}>انصراف</button>
            <button type="submit" className={modalUi.btnPrimary} disabled={props.isSaving}>
              {props.editing ? 'ذخیره تغییرات' : 'ایجاد کوپن'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

function CouponDetails(props: {
  coupon: Coupon
  catalogStores: CatalogStoreListItem[]
  catalogStoresLoading: boolean
}) {
  const [usageUserId, setUsageUserId] = useState('')
  const [testSiteId, setTestSiteId] = useState('')
  const [testUserId, setTestUserId] = useState('')
  const [testQty, setTestQty] = useState('1')
  const [testSku, setTestSku] = useState('')
  const [testResult, setTestResult] = useState<QuoteResponse | null>(null)
  const [testError, setTestError] = useState<string | null>(null)
  const [testing, setTesting] = useState(false)

  const testedLine = useMemo(() => {
    if (!testResult || !testSku) return null
    return testResult.lines.find(l => !l.isGift && l.skuId === testSku) ?? null
  }, [testResult, testSku])

  const testedCouponView = useMemo(() => {
    if (!testedLine) return null

    const couponPerUnitDeltaRaw = (testedLine.adjustments ?? [])
      .filter(a => a.sourceType === 'coupon' && a.sourceId === props.coupon.id)
      .reduce((sum, a) => sum + a.amount, 0)

    const guardrailCapPerUnitDelta = (testedLine.adjustments ?? [])
      .filter(a => a.sourceType === 'guardrail' && a.sourceId === 'cap')
      .reduce((sum, a) => sum + a.amount, 0)

    const shouldAttributeCapToCoupon =
      couponPerUnitDeltaRaw !== 0
      && guardrailCapPerUnitDelta !== 0
      && !!(props.coupon.guardrails?.maxDiscountAmount || props.coupon.guardrails?.maxDiscountPercent)

    const couponPerUnitDeltaEffective = couponPerUnitDeltaRaw + (shouldAttributeCapToCoupon ? guardrailCapPerUnitDelta : 0)

    const beforeCouponUnitPrice = testedLine.finalUnitPrice - couponPerUnitDeltaEffective
    const couponDiscountTotal = Math.max(0, -couponPerUnitDeltaEffective * testedLine.quantity)

    return {
      beforeCouponUnitPrice,
      afterCouponUnitPrice: testedLine.finalUnitPrice,
      couponDiscountTotal,
      attributedGuardrailCap: shouldAttributeCapToCoupon ? guardrailCapPerUnitDelta : 0,
    }
  }, [props.coupon.guardrails, props.coupon.id, testedLine])

  const selectedQty = testedLine?.quantity ?? 0
  const globalUnitPrice = testedLine?.baseUnitPrice ?? 0
  const globalBaseTotal = globalUnitPrice * selectedQty

  const couponDiscountHint = useMemo(() => {
    if (!testedCouponView || !testResult) return ''
    return formatCouponDiscountHint(props.coupon, testedCouponView.couponDiscountTotal, testResult.currency)
  }, [props.coupon, testResult, testedCouponView])

  const discountBreakdown = useMemo(() => (testResult ? computeDiscountBreakdown(testResult) : []), [testResult])

  const { data: usage, isLoading: usageLoading, refetch: refetchUsage } = useCouponUsage(
    props.coupon.id,
    usageUserId.trim() || undefined,
  )
  const { data: reservations, isLoading: reservationsLoading, refetch: refetchReservations } = useCouponReservations(props.coupon.id, true)

  const runTest = async () => {
    if (!testSiteId) {
      swalToastError('سایت را انتخاب کنید.')
      return
    }
    if (!testSku) {
      swalToastError('محصول را انتخاب کنید.')
      return
    }
    const qty = Number.parseInt(testQty || '1', 10)
    if (!Number.isFinite(qty) || qty <= 0) {
      swalToastError('تعداد باید عدد معتبر باشد.')
      return
    }
    setTesting(true)
    setTestError(null)
    setTestResult(null)
    try {
      const quote = await createQuote({
        siteId: testSiteId,
        userId: testUserId.trim() || null,
        couponCode: props.coupon.code,
        timestamp: null,
        items: [{ skuId: testSku, qty, batchId: null }],
      })
      setTestResult(quote)
    } catch (err) {
      setTestError(getApiErrorMessage(err, 'خطا در محاسبه قیمت.'))
    } finally {
      setTesting(false)
    }
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-3 text-xs">
      <div className="border rounded-md p-3 bg-white space-y-2">
        <div className="flex items-center justify-between">
          <div className="font-semibold">وضعیت استفاده</div>
          <button className="btn-secondary px-3 py-1 rounded" onClick={() => refetchUsage()} disabled={usageLoading}>بازخوانی</button>
        </div>
        <label className="text-xs text-gray-600">UserId (اختیاری) برای محدودیت هر کاربر</label>
        <input className="input text-xs" value={usageUserId} onChange={e => setUsageUserId(e.target.value)} placeholder="اختیاری" />
        {usageLoading ? <Spinner /> : (
          <div className="space-y-1">
            <div>استفاده شده کل: {usage?.redeemedTotal ?? 0}</div>
            <div>رزرو فعال کل: {usage?.reservedActiveTotal ?? 0}</div>
            <div>استفاده شده هر کاربر: {usage?.redeemedByUser ?? 0}</div>
            <div>رزرو فعال هر کاربر: {usage?.reservedActiveByUser ?? 0}</div>
          </div>
        )}
      </div>

      <div className="border rounded-md p-3 bg-white space-y-2">
        <div className="flex items-center justify-between">
          <div className="font-semibold">رزروهای فعال</div>
          <button className="btn-secondary px-3 py-1 rounded" onClick={() => refetchReservations()} disabled={reservationsLoading}>بازخوانی</button>
        </div>
        {reservationsLoading ? <Spinner /> : (
          <div className="space-y-2">
            {(reservations ?? []).length === 0 ? (
              <div className="text-gray-500">رزرو فعالی وجود ندارد.</div>
            ) : (
              <ul className="space-y-2">
                {(reservations ?? []).map(r => (
                  <li key={r.id} className="border rounded-md p-2">
                    <div>کاربر: {r.userId ?? '-'}</div>
                    <div>سایت: {r.siteId ?? '-'}</div>
                    <div>انقضا: {formatJalaliDate(r.expiresAt)} {formatTime(r.expiresAt)}</div>
                    {r.cartHash ? <div className="text-gray-600">CartHash: {r.cartHash}</div> : null}
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}
      </div>

      <div className="border rounded-md p-3 bg-white space-y-2">
        <div className="font-semibold">تست قیمت</div>
        <div>
          <label className="label">سایت</label>
          <select className="input text-xs" value={testSiteId} onChange={e => setTestSiteId(e.target.value)} disabled={props.catalogStoresLoading}>
            <option value="">{props.catalogStoresLoading ? 'در حال دریافت سایت ها...' : 'سایت را انتخاب کنید'}</option>
            {props.catalogStores.map(s => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
        </div>
        <div>
          <label className="label">UserId (اختیاری)</label>
          <input className="input text-xs" value={testUserId} onChange={e => setTestUserId(e.target.value)} placeholder="Guid اختیاری" />
        </div>
        <SkuPicker
          label="محصول/واریانت"
          value={testSku}
          onChange={(next) => setTestSku(next.skuId)}
          placeholder="جستجو کنید..."
          showLabel
        />
        <div>
          <label className="label">تعداد</label>
          <input className="input text-xs" type="number" value={testQty} onChange={e => setTestQty(e.target.value)} />
        </div>
        <button className="btn w-full" type="button" onClick={runTest} disabled={testing}>
          {testing ? 'در حال محاسبه...' : 'محاسبه قیمت'}
        </button>
        {testError ? <div className="text-red-600">{testError}</div> : null}
        {testResult ? (
          <div className="text-xs space-y-1">
            <div>قیمت واحد گلوبال محصول: {globalUnitPrice.toLocaleString('fa-IR')}</div>
            <div>تعداد انتخاب شده: {selectedQty.toLocaleString('fa-IR')}</div>
            <div>جمع قیمت پایه گلوبال: {globalBaseTotal.toLocaleString('fa-IR')}</div>
            <div>تخفیف کل (قیمت ویژه/کمپین/کوپن): {testResult.discountTotal.toLocaleString('fa-IR')}</div>
            <div>قیمت نهایی: {testResult.finalTotal.toLocaleString('fa-IR')}</div>

            {testedCouponView ? (
              <div className="mt-2 border rounded-md bg-white p-2 space-y-1">
                <div className="font-semibold">جزئیات کوپن برای همین آیتم.</div>
                <div>قیمت واحد محصول قبل از کوپن (در سایت انتخاب شده): {testedCouponView.beforeCouponUnitPrice.toLocaleString('fa-IR')}</div>
                <div>تعداد انتخاب شده: {selectedQty.toLocaleString('fa-IR')}</div>
                <div>
                  تخفیف کوپن: {testedCouponView.couponDiscountTotal.toLocaleString('fa-IR')}
                  {couponDiscountHint ? ` ${couponDiscountHint}` : ''}
                </div>
                <div>قیمت نهایی بعد از تخفیف: {(testedCouponView.afterCouponUnitPrice * selectedQty).toLocaleString('fa-IR')}</div>
              </div>
            ) : null}

            {discountBreakdown.length ? (
              <div className="mt-2 border rounded-md bg-white p-2 space-y-1">
                <div className="font-semibold">تفکیک تاثیر منابع (کل)</div>
                <ul className="space-y-1">
                  {discountBreakdown.map(x => (
                    <li key={x.key} className="flex items-center justify-between gap-2">
                      <span className="text-gray-700">{x.label}</span>
                      <span className={`font-mono ${x.totalDelta < 0 ? 'text-emerald-700' : 'text-red-700'}`}>
                        {x.totalDelta.toLocaleString('fa-IR')}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
            {testResult.rejectedSources?.length ? (
              <div className="mt-2">
                <div className="font-semibold mb-1 text-red-700">دلایل رد</div>
                <RejectedSourcesPanel rejectedSources={testResult.rejectedSources} />
              </div>
            ) : null}
          </div>
        ) : null}
      </div>
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

function isActiveNow(c: Coupon) {
  if (!c.isActive) return false
  const now = new Date()
  if (c.validFrom && new Date(c.validFrom) > now) return false
  if (c.validTo && new Date(c.validTo) < now) return false
  return true
}

function formatRange(from?: string | null, to?: string | null) {
  const start = formatJalaliDate(from)
  const end = formatJalaliDate(to)
  if (!start && !end) return '-'
  return `${start || '-'} تا ${end || '-'}`
}

function benefitLabel(benefit: BenefitDefinition) {
  switch (benefit.kind) {
    case 'percentOff':
      return `تخفیف درصدی ${benefit.percent}%`
    case 'amountOff':
      return `تخفیف مبلغی ${benefit.amount?.toLocaleString('fa-IR')}`
    case 'fixedPrice':
      return `قیمت ثابت ${benefit.price?.toLocaleString('fa-IR')}`
    case 'cashbackPercent':
      return `کش بک درصدی ${benefit.percent}%`
    case 'cashbackAmount':
      return `کش بک مبلغی ${benefit.amount?.toLocaleString('fa-IR')}`
    case 'bundleFixedPrice':
      return 'باندل قیمت ثابت'
    case 'buyXGetY':
      return `بخر ${benefit.buyQty} بگیر ${benefit.getQty}`
    default:
      return benefit.kind
  }
}

function formatTime(input?: string | null) {
  if (!input) return ''
  const d = new Date(input)
  if (Number.isNaN(d.getTime())) return ''
  return d.toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' })
}

type DiscountBreakdownItem = { key: string; label: string; totalDelta: number }

function computeDiscountBreakdown(quote: QuoteResponse): DiscountBreakdownItem[] {
  const totals: Record<string, number> = {
    override: 0,
    promotion: 0,
    coupon: 0,
    guardrail: 0,
  }

  for (const line of quote.lines ?? []) {
    if (line.isGift) continue
    const qty = line.quantity ?? 0
    if (qty <= 0) continue

    for (const adj of line.adjustments ?? []) {
      if (!adj?.sourceType) continue
      if (!(adj.sourceType in totals)) continue
      totals[adj.sourceType] += (adj.amount ?? 0) * qty
    }
  }

  const items: DiscountBreakdownItem[] = [
    { key: 'override', label: 'قیمت ویژه', totalDelta: totals.override },
    { key: 'promotion', label: 'کمپین ها', totalDelta: totals.promotion },
    { key: 'coupon', label: 'کوپن', totalDelta: totals.coupon },
    { key: 'guardrail', label: 'گاردریل (سقف/کف)', totalDelta: totals.guardrail },
  ]

  return items.filter(x => x.totalDelta !== 0)
}

function formatCouponDiscountHint(coupon: Coupon, couponDiscountTotal: number, currency: string) {
  const maxAmount = coupon.guardrails?.maxDiscountAmount
  if (typeof maxAmount === 'number' && Number.isFinite(maxAmount) && maxAmount > 0) {
    if (couponDiscountTotal >= maxAmount - 0.000001) return '(حداکثر)'
  }

  switch (coupon.benefit.kind) {
    case 'percentOff':
      return `(${coupon.benefit.percent}٪)`
    case 'amountOff': {
      const amount = coupon.benefit.amount
      const label = currencyText(coupon.benefit.currency ?? currency)
      return `(${amount.toLocaleString('fa-IR')} ${label})`
    }
    case 'fixedPrice':
      return '(قیمت ثابت)'
    default:
      return ''
  }
}

function currencyText(currency: string) {
  const c = (currency || '').toUpperCase()
  if (c === 'IRR') return 'ریال'
  if (c === 'IRT') return 'تومان'
  return c || 'ریال'
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




