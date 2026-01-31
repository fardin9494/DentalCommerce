import { useMemo, useState } from 'react'
import type { QuoteSourceResult } from '../types'

type SourceFilter = 'relevant' | 'all' | 'coupon'

export function RejectedSourcesPanel(props: { rejectedSources: QuoteSourceResult[] }) {
  const [filter, setFilter] = useState<SourceFilter>('relevant')

  const couponRejectedCount = useMemo(
    () => props.rejectedSources.filter(s => s.sourceType === 'coupon').length,
    [props.rejectedSources],
  )

  const relevantRejectedCount = useMemo(
    () => props.rejectedSources.filter(isRelevantRejection).length,
    [props.rejectedSources],
  )

  const filtered = useMemo(() => {
    if (filter === 'coupon') return props.rejectedSources.filter(s => s.sourceType === 'coupon')
    if (filter === 'relevant') return props.rejectedSources.filter(isRelevantRejection)
    return props.rejectedSources
  }, [filter, props.rejectedSources])

  if (props.rejectedSources.length === 0) {
    return <div className="text-gray-600">موردی وجود ندارد.</div>
  }

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={`btn-secondary px-3 py-1 rounded ${filter === 'all' ? 'bg-gray-200' : ''}`}
            onClick={() => setFilter('all')}
          >
            همه ({props.rejectedSources.length})
          </button>
          <button
            type="button"
            className={`btn-secondary px-3 py-1 rounded ${filter === 'relevant' ? 'bg-gray-200' : ''}`}
            onClick={() => setFilter('relevant')}
          >
            مرتبط ({relevantRejectedCount})
          </button>
          <button
            type="button"
            className={`btn-secondary px-3 py-1 rounded ${filter === 'coupon' ? 'bg-gray-200' : ''}`}
            onClick={() => setFilter('coupon')}
          >
            فقط کوپن ({couponRejectedCount})
          </button>
        </div>
        <div className="text-xs text-gray-500">
          مرتبط: مواردی که احتمالاً روی همین Quote اثر دارند (ردهای «عدم تطابق با سبد» پنهان می‌شوند).
        </div>
      </div>

      <ul className="space-y-2">
        {filtered.map((s, idx) => (
          <li key={`${s.sourceType ?? 'unknown'}-${s.sourceId ?? 'unknown'}-${idx}`} className="border rounded-md bg-white p-2">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="flex flex-wrap items-center gap-2">
                <span className={`badge ${badgeClass(s.sourceType)}`}>{sourceTypeLabel(s.sourceType)}</span>
                <span className="font-semibold">{s.name}</span>
                <span className="text-xs text-gray-500 font-mono">{s.sourceId}</span>
              </div>
              <div className="text-xs text-gray-500">
                {s.priority != null ? <>اولویت: {s.priority}</> : null}
                {s.priority != null && s.stackingGroup ? <span> · </span> : null}
                {s.stackingGroup ? <>گروه: {s.stackingGroup}</> : null}
              </div>
            </div>

            <div className="mt-1 text-sm text-gray-800">{s.reason || 'بدون دلیل'}</div>
            {reasonHint(s.reason) ? <div className="mt-1 text-xs text-gray-600">{reasonHint(s.reason)}</div> : null}
          </li>
        ))}
      </ul>
    </div>
  )
}

function sourceTypeLabel(sourceType: string) {
  switch (sourceType) {
    case 'coupon':
      return 'کوپن'
    case 'promotion':
      return 'کمپین'
    case 'override':
      return 'قیمت ویژه'
    default:
      return sourceType
  }
}

function badgeClass(sourceType: string) {
  switch (sourceType) {
    case 'coupon':
      return 'badge-green'
    case 'promotion':
      return 'badge-gray'
    case 'override':
      return 'badge-gray'
    default:
      return 'badge-gray'
  }
}

function reasonHint(reason?: string | null) {
  if (!reason) return null
  switch (reason) {
    case 'No matching SKU for product eligibility.':
      return 'شرط «محصول» فقط روی SKUهای مشخص اعمال می‌شود؛ SKU آیتم داخل Quote با لیست SKUهای این شرط هم‌خوانی ندارد.'
    case 'Coupon blocked by promotion.':
      return 'حداقل یک کمپین انتخاب‌شده اجازه ترکیب با کوپن را نمی‌دهد (CombinableWithCoupons=false).'
    case 'Not combinable with other promotions.':
      return 'این منبع با سایر کمپین‌ها قابل ترکیب نیست و به همین دلیل رد شده است.'
    case 'Not selected by stacking policy.':
      return 'طبق سیاست تجمیع (StackingMode)، این منبع در بین گزینه‌ها انتخاب نشده است.'
    default:
      return null
  }
}

function isRelevantRejection(s: QuoteSourceResult) {
  if (s.sourceType === 'coupon') return true
  if (s.sourceType !== 'promotion') return true
  return !isCartMismatchReason(s.reason)
}

function isCartMismatchReason(reason?: string | null) {
  if (!reason) return false
  const normalized = reason.trim()
  return (
    normalized === 'Unknown eligibility type.'
    || normalized === 'No matching SKU for product eligibility.'
    || normalized === 'No matching category in cart.'
    || normalized === 'No matching brand in cart.'
    || normalized === 'No matching tag in cart.'
    || normalized === 'Site not eligible.'
    || normalized === 'User not provided.'
    || normalized === 'User not eligible.'
    || normalized === 'Outside campaign date range.'
    || normalized === 'Bundle requirements not met.'
    || normalized === 'Batch expiry threshold not configured.'
    || normalized === 'No batch matches expiry criteria.'
    || normalized === 'Eligibility condition failed.'
    || normalized === 'Eligibility conditions produced no matching items.'
    || normalized === 'No eligibility condition matched.'
  )
}
