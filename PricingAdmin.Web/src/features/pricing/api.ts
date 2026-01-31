import { fetchJson, toQuery } from '@/lib/api/client'
import type { ApiError } from '@/lib/api/types'
import type {
  PriceList,
  QuoteRequest,
  QuoteResponse,
  StackingMode,
  PricingPolicy,
  PriceOverride,
  OverrideScope,
  OverrideType,
  PromotionCampaign,
  Coupon,
  CouponUsage,
  CouponReservation,
  Guardrails,
  EligibilityDefinition,
  BenefitDefinition,
} from './types'

export async function getPricingPolicy(siteId?: string | null): Promise<PricingPolicy | null> {
  try {
    const data = await fetchJson<PricingPolicy>(`/pricing/policies${toQuery({ siteId })}`)
    return data
  } catch (err: any) {
    if (err?.status === 404) return null
    throw err
  }
}

export async function listPricingPolicies(): Promise<PricingPolicy[]> {
  return fetchJson<PricingPolicy[]>(`/pricing/policies/list`)
}

export async function upsertPricingPolicy(input: { siteId?: string | null; defaultStackingMode: StackingMode }) {
  return fetchJson<{ id: string }>(`/pricing/policies`, { method: 'PUT', json: input })
}

export async function listPriceLists(): Promise<PriceList[]> {
  return fetchJson<PriceList[]>(`/pricing/pricelists`)
}

export async function getPriceList(id: string): Promise<PriceList> {
  return fetchJson<PriceList>(`/pricing/pricelists/${id}`)
}

export async function createPriceList(input: { name: string; currency: string; validFrom?: string | null; validTo?: string | null; isActive: boolean }) {
  return fetchJson<{ id: string }>(`/pricing/pricelists`, { method: 'POST', json: input })
}

export async function updatePriceList(
  id: string,
  input: { name: string; currency: string; validFrom?: string | null; validTo?: string | null; isActive: boolean },
) {
  return fetchJson<void>(`/pricing/pricelists/${id}`, { method: 'PUT', json: input })
}

export async function deletePriceList(id: string) {
  return fetchJson<void>(`/pricing/pricelists/${id}`, { method: 'DELETE' })
}

export async function bulkUpdatePrices(input: {
  priceListId?: string | null
  currency: string
  items: Array<{ skuId: string; basePrice: number; tierPrices?: Array<{ minQty: number; unitPrice: number }> }>
}) {
  return fetchJson<{ updated: number }>(`/pricing/bulk/prices`, { method: 'POST', json: input })
}

export async function createQuote(input: QuoteRequest): Promise<QuoteResponse> {
  return fetchJson<QuoteResponse>(`/pricing/quote`, { method: 'POST', json: input })
}

export async function listOverrides(params: { scopeType?: OverrideScope; scopeId?: string; skuId?: string }): Promise<PriceOverride[]> {
  return fetchJson<PriceOverride[]>(`/pricing/overrides${toQuery(params)}`)
}

export async function getOverride(id: string): Promise<PriceOverride> {
  return fetchJson<PriceOverride>(`/pricing/overrides/${id}`)
}

export async function createOverride(input: {
  scopeType: OverrideScope
  scopeId: string
  skuId: string
  overrideType: OverrideType
  value: number
  currency: string
  validFrom?: string | null
  validTo?: string | null
  priority: number
  stackingGroup: string
}) {
  return fetchJson<{ id: string }>(`/pricing/overrides`, { method: 'POST', json: input })
}

export async function updateOverride(
  id: string,
  input: {
    overrideType: OverrideType
    value: number
    currency: string
    validFrom?: string | null
    validTo?: string | null
    priority: number
    stackingGroup: string
  },
) {
  return fetchJson<void>(`/pricing/overrides/${id}`, { method: 'PUT', json: input })
}

export async function deleteOverride(id: string) {
  return fetchJson<void>(`/pricing/overrides/${id}`, { method: 'DELETE' })
}

export async function listCampaigns(params: { isActive?: boolean }): Promise<PromotionCampaign[]> {
  return fetchJson<PromotionCampaign[]>(`/pricing/campaigns${toQuery(params)}`)
}

export async function getCampaign(id: string): Promise<PromotionCampaign> {
  return fetchJson<PromotionCampaign>(`/pricing/campaigns/${id}`)
}

export async function createCampaign(input: {
  name: string
  isActive: boolean
  validFrom?: string | null
  validTo?: string | null
  priority: number
  stackingGroup: string
  stackingMode: StackingMode
  combinableWithOtherPromotions: boolean
  combinableWithCoupons: boolean
  exclusiveGroup?: string | null
  guardrails?: Guardrails | null
  eligibility: EligibilityDefinition
  benefit: BenefitDefinition
}) {
  return fetchJson<{ id: string }>(`/pricing/campaigns`, { method: 'POST', json: input })
}

export async function updateCampaign(
  id: string,
  input: {
    name: string
    isActive: boolean
    validFrom?: string | null
    validTo?: string | null
    priority: number
    stackingGroup: string
    stackingMode: StackingMode
    combinableWithOtherPromotions: boolean
    combinableWithCoupons: boolean
    exclusiveGroup?: string | null
    guardrails?: Guardrails | null
    eligibility: EligibilityDefinition
    benefit: BenefitDefinition
  },
) {
  return fetchJson<void>(`/pricing/campaigns/${id}`, { method: 'PUT', json: input })
}

export async function deleteCampaign(id: string) {
  return fetchJson<void>(`/pricing/campaigns/${id}`, { method: 'DELETE' })
}

export async function listCoupons(params: { isActive?: boolean; code?: string }): Promise<Coupon[]> {
  return fetchJson<Coupon[]>(`/pricing/coupons${toQuery(params)}`)
}

export async function getCouponUsage(id: string, userId?: string): Promise<CouponUsage> {
  return fetchJson<CouponUsage>(`/pricing/coupons/${id}/usage${toQuery({ userId })}`)
}

export async function listCouponReservations(id: string, activeOnly = true): Promise<CouponReservation[]> {
  return fetchJson<CouponReservation[]>(`/pricing/coupons/${id}/reservations${toQuery({ activeOnly })}`)
}

export async function getCoupon(id: string): Promise<Coupon> {
  return fetchJson<Coupon>(`/pricing/coupons/${id}`)
}

export async function createCoupon(input: {
  code: string
  name?: string | null
  isActive: boolean
  validFrom?: string | null
  validTo?: string | null
  maxUsesTotal?: number | null
  maxUsesPerUser?: number | null
  priority: number
  combinableWithPromotions: boolean
  exclusiveGroup?: string | null
  guardrails?: Guardrails | null
  eligibility: EligibilityDefinition
  benefit: BenefitDefinition
}) {
  return fetchJson<{ id: string }>(`/pricing/coupons`, { method: 'POST', json: input })
}

export async function updateCoupon(
  id: string,
  input: {
    code: string
    name?: string | null
    isActive: boolean
    validFrom?: string | null
    validTo?: string | null
    maxUsesTotal?: number | null
    maxUsesPerUser?: number | null
    priority: number
    combinableWithPromotions: boolean
    exclusiveGroup?: string | null
    guardrails?: Guardrails | null
    eligibility: EligibilityDefinition
    benefit: BenefitDefinition
  },
) {
  return fetchJson<void>(`/pricing/coupons/${id}`, { method: 'PUT', json: input })
}

export async function deleteCoupon(id: string) {
  return fetchJson<void>(`/pricing/coupons/${id}`, { method: 'DELETE' })
}

export async function bulkCreateCategoryCampaign(input: {
  categoryIds: string[]
  categories?: Array<{ id: string; name: string }>
  namePrefix: string
  isActive: boolean
  validFrom?: string | null
  validTo?: string | null
  priority: number
  stackingGroup?: string | null
  stackingMode: StackingMode
  combinableWithOtherPromotions: boolean
  combinableWithCoupons: boolean
  exclusiveGroup?: string | null
  guardrails?: Guardrails | null
  benefit: BenefitDefinition
}) {
  return fetchJson<{ created: number }>(`/pricing/bulk/campaigns/category`, { method: 'POST', json: input })
}

export function getApiErrorMessage(err: unknown, fallback: string) {
  if (!err || typeof err !== 'object') return fallback
  const apiErr = err as ApiError & { details?: any }

  if (apiErr.details && typeof apiErr.details === 'object') {
    const details = apiErr.details as any
    if (details.errors && typeof details.errors === 'object') {
      const parts: string[] = []
      Object.values(details.errors).forEach((vals) => {
        if (Array.isArray(vals)) parts.push(...vals.map(v => String(v)))
      })
      if (parts.length > 0) return parts.join('، ')
    }
    if (details.detail) return String(details.detail)
    if (details.message) return String(details.message)
  }

  if (apiErr.message) return apiErr.message
  if (err instanceof Error) return err.message
  return fallback
}
