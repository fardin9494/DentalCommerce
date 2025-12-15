import { z } from 'zod'

export const AdjustmentStatusSchema = z.enum(['Draft', 'Posted', 'Canceled'])
export type AdjustmentStatus = z.infer<typeof AdjustmentStatusSchema>

export const AdjustmentReasonSchema = z.enum([
  'InitialBalance',
  'Damage',
  'Expired',
  'Found',
  'Shrinkage',
  'Correction',
  'Other',
])
export type AdjustmentReason = z.infer<typeof AdjustmentReasonSchema>

// Status and Reason number mappings (for API)
export const AdjustmentStatusMap: Record<AdjustmentStatus, number> = {
  Draft: 1,
  Posted: 2,
  Canceled: 3,
}

export const AdjustmentReasonMap: Record<AdjustmentReason, number> = {
  InitialBalance: 1,
  Damage: 2,
  Expired: 3,
  Found: 4,
  Shrinkage: 5,
  Correction: 6,
  Other: 99,
}

// Persian labels
export const AdjustmentStatusLabels: Record<AdjustmentStatus, string> = {
  Draft: 'پیش‌نویس',
  Posted: 'ثبت شده',
  Canceled: 'لغو شده',
}

export const AdjustmentReasonLabels: Record<AdjustmentReason, string> = {
  InitialBalance: 'موجودی اولیه',
  Damage: 'خرابی/آسیب',
  Expired: 'انقضا',
  Found: 'یافت‌شده',
  Shrinkage: 'کسری/کمبود',
  Correction: 'اصلاح موجودی',
  Other: 'سایر',
}

// Status colors for badges
export const AdjustmentStatusColors: Record<AdjustmentStatus, string> = {
  Draft: 'bg-yellow-100 text-yellow-800',
  Posted: 'bg-green-100 text-green-800',
  Canceled: 'bg-red-100 text-red-800',
}

export const AdjustmentLineSchema = z.object({
  id: z.string().uuid(),
  lineNo: z.number(),
  stockItemId: z.string().uuid(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDateUtc: z.string().nullable().optional(),
  qtyDelta: z.number(), // + increase / - decrease
})
export type AdjustmentLine = z.infer<typeof AdjustmentLineSchema>

export const AdjustmentDetailSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  status: AdjustmentStatusSchema,
  reason: AdjustmentReasonSchema,
  note: z.string().nullable().optional(),
  docDate: z.string(),
  postedAt: z.string().nullable().optional(),
  lines: z.array(AdjustmentLineSchema),
})
export type AdjustmentDetail = z.infer<typeof AdjustmentDetailSchema>

export const AdjustmentListItemSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  warehouseName: z.string().nullable().optional(),
  status: z.string(),
  reason: z.string(),
  note: z.string().nullable().optional(),
  docDate: z.string(),
  postedAt: z.string().nullable().optional(),
  linesCount: z.number(),
  totalQtyDelta: z.number(),
  createdAt: z.string(),
})
export type AdjustmentListItem = z.infer<typeof AdjustmentListItemSchema>

export const AdjustmentsListResultSchema = z.object({
  items: z.array(AdjustmentListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type AdjustmentsListResult = z.infer<typeof AdjustmentsListResultSchema>

export interface AdjustmentsListFilters {
  warehouseId?: string
  status?: number
  reason?: number
  fromDate?: string
  toDate?: string
  search?: string
  page?: number
  pageSize?: number
}

export const CreateAdjustmentSchema = z.object({
  warehouseId: z.string().uuid(),
  reason: z.number().min(1).max(99),
  note: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type CreateAdjustmentDto = z.infer<typeof CreateAdjustmentSchema>

export const AddAdjustmentLineSchema = z.object({
  stockItemId: z.string().uuid(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().optional().nullable(),
  lotNumber: z.string().optional().nullable(),
  expiryDateUtc: z.string().optional().nullable(),
  qtyDelta: z.number().refine((val) => val !== 0, { message: 'مقدار نمی‌تواند صفر باشد' }),
})
export type AddAdjustmentLineDto = z.infer<typeof AddAdjustmentLineSchema>

export const UpdateAdjustmentHeaderSchema = z.object({
  note: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type UpdateAdjustmentHeaderDto = z.infer<typeof UpdateAdjustmentHeaderSchema>

export const UpdateAdjustmentLineSchema = z.object({
  qtyDelta: z.number().refine((val) => val !== 0, { message: 'مقدار نمی‌تواند صفر باشد' }),
})
export type UpdateAdjustmentLineDto = z.infer<typeof UpdateAdjustmentLineSchema>
