export type StackingMode = 'Cascading' | 'BestPrice' | 'PriorityOnly' | 'BestOfEachGroup'

export type OverrideScope = 'Site' | 'User'
export type OverrideType = 'PercentOff' | 'AmountOff' | 'FixedPrice'

export type PricingPolicy = {
  id: string
  siteId?: string | null
  defaultStackingMode: StackingMode
}

export type TierPrice = {
  minQty: number
  unitPrice: number
}

export type PriceListItem = {
  id: string
  skuId: string
  basePrice: number
  tierPrices: TierPrice[]
}

export type PriceList = {
  id: string
  name: string
  currency: string
  validFrom?: string | null
  validTo?: string | null
  isActive: boolean
  items: PriceListItem[]
}

export type Guardrails = {
  maxDiscountPercent?: number | null
  maxDiscountAmount?: number | null
}

export type EligibilityDefinition =
  | { kind: 'all' }
  | { kind: 'allOf'; conditions: EligibilityDefinition[] }
  | { kind: 'anyOf'; conditions: EligibilityDefinition[] }
  | { kind: 'product'; skuIds: string[] }
  | { kind: 'category'; categoryIds: string[] }
  | { kind: 'brand'; brandIds: string[] }
  | { kind: 'tag'; tags: string[] }
  | { kind: 'site'; siteIds: string[] }
  | { kind: 'user'; userIds: string[] }
  | { kind: 'seasonal'; from?: string | null; to?: string | null }
  | { kind: 'bundle'; requirements: Array<{ skuId: string; qty: number }> }
  | { kind: 'batchExpiryBefore'; date?: string | null; withinDays?: number | null }

export type BenefitDefinition =
  | { kind: 'percentOff'; percent: number }
  | { kind: 'amountOff'; amount: number; currency?: string | null }
  | { kind: 'fixedPrice'; price: number; currency?: string | null }
  | { kind: 'bundleFixedPrice'; requiredItems: Array<{ skuId: string; qtyRequired: number }>; bundlePrice: number; currency?: string | null; skuIds?: string[] }
  | { kind: 'buyXGetY'; buySkuId: string; buyQty: number; getSkuId: string; getQty: number }
  | { kind: 'cashbackPercent'; percent: number }
  | { kind: 'cashbackAmount'; amount: number; currency?: string | null }

export type PromotionCampaign = {
  id: string
  name: string
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
  eligibility: EligibilityDefinition
  benefit: BenefitDefinition
}

export type Coupon = {
  id: string
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
}

export type CouponUsage = {
  couponId: string
  redeemedTotal: number
  reservedActiveTotal: number
  redeemedByUser: number
  reservedActiveByUser: number
}

export type CouponReservation = {
  id: string
  reservationId?: string | null
  userId?: string | null
  siteId?: string | null
  cartHash?: string | null
  expiresAt?: string | null
  redeemedAt?: string | null
  createdAt: string
}

export type PriceOverride = {
  id: string
  scopeType: OverrideScope
  scopeId: string
  skuId: string
  overrideType: OverrideType
  value: number
  currency: string
  validFrom?: string | null
  validTo?: string | null
  priority: number
  stackingGroup?: string | null
}

export type QuoteRequest = {
  siteId: string
  userId?: string | null
  couponCode?: string | null
  timestamp?: string | null
  items: Array<{
    skuId: string
    qty: number
    batchId?: string | null
  }>
}

export type PriceAdjustment = {
  sourceType: string
  sourceId: string
  description: string
  amount: number
  isCashback?: boolean
}

export type QuoteLine = {
  skuId: string
  batchId?: string | null
  quantity: number
  baseUnitPrice: number
  finalUnitPrice: number
  isGift: boolean
  adjustments: PriceAdjustment[]
}

export type QuoteSourceResult = {
  sourceType: string
  sourceId: string
  name: string
  applied: boolean
  reason?: string | null
  priority?: number | null
  stackingGroup?: string | null
}

export type QuoteTraceEntry = {
  stage: string
  message: string
}

export type QuoteResponse = {
  siteId: string
  userId?: string | null
  currency: string
  timestamp: string
  subtotal: number
  discountTotal: number
  finalTotal: number
  cashbackTotal: number
  lines: QuoteLine[]
  appliedSources: QuoteSourceResult[]
  rejectedSources: QuoteSourceResult[]
  trace: QuoteTraceEntry[]
}
