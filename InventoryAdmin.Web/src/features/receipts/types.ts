import { z } from 'zod'

export const ReceiptStatusSchema = z.enum(['Draft', 'Received', 'Approved', 'Canceled'])
export type ReceiptStatus = z.infer<typeof ReceiptStatusSchema>

export const ReceiptReasonSchema = z.enum(['Purchase', 'ReturnIn', 'Production', 'Other'])
export type ReceiptReason = z.infer<typeof ReceiptReasonSchema>

// Status and Reason number mappings (for API)
export const ReceiptStatusMap: Record<ReceiptStatus, number> = {
  Draft: 1,
  Received: 2,
  Approved: 3,
  Canceled: 4,
}

export const ReceiptReasonMap: Record<ReceiptReason, number> = {
  Purchase: 1,
  ReturnIn: 2,
  Production: 3,
  Other: 99,
}

// Persian labels
export const ReceiptStatusLabels: Record<ReceiptStatus, string> = {
  Draft: 'پیش‌نویس',
  Received: 'دریافت شده',
  Approved: 'تایید شده',
  Canceled: 'لغو شده',
}

export const ReceiptReasonLabels: Record<ReceiptReason, string> = {
  Purchase: 'خرید',
  ReturnIn: 'مرجوعی',
  Production: 'تولید',
  Other: 'سایر',
}

// Status colors for badges
export const ReceiptStatusColors: Record<ReceiptStatus, string> = {
  Draft: 'bg-yellow-100 text-yellow-800',
  Received: 'bg-blue-100 text-blue-800',
  Approved: 'bg-green-100 text-green-800',
  Canceled: 'bg-red-100 text-red-800',
}

export const ReceiptLineSchema = z.object({
  id: z.string().uuid(),
  lineNo: z.number(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  qty: z.number(),
  lotNumber: z.string().nullable().optional(),
  expiryDateUtc: z.string().nullable().optional(),
  unitCost: z.number().nullable().optional(),
})
export type ReceiptLine = z.infer<typeof ReceiptLineSchema>

export const ReceiptDetailSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  status: ReceiptStatusSchema,
  reason: ReceiptReasonSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string(),
  receivedAt: z.string().nullable().optional(),
  approvedAt: z.string().nullable().optional(),
  lines: z.array(ReceiptLineSchema),
})
export type ReceiptDetail = z.infer<typeof ReceiptDetailSchema>

export const CreateReceiptSchema = z.object({
  warehouseId: z.string().uuid(),
  reason: z.number().min(1).max(99),
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type CreateReceiptDto = z.infer<typeof CreateReceiptSchema>

export const AddReceiptLineSchema = z.object({
  productId: z.string().uuid(),
  variantId: z.string().uuid().optional().nullable(),
  qty: z.number().positive(),
  lotNumber: z.string().optional().nullable(),
  expiryDateUtc: z.string().optional().nullable(),
  unitCost: z.number().positive().optional().nullable(),
})
export type AddReceiptLineDto = z.infer<typeof AddReceiptLineSchema>

export const UpdateReceiptHeaderSchema = z.object({
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type UpdateReceiptHeaderDto = z.infer<typeof UpdateReceiptHeaderSchema>

export const UpdateReceiptLineSchema = z.object({
  qty: z.number().positive().optional(),
  lotNumber: z.string().optional().nullable(),
  expiryDateUtc: z.string().optional().nullable(),
  unitCost: z.number().positive().optional().nullable(),
})
export type UpdateReceiptLineDto = z.infer<typeof UpdateReceiptLineSchema>

// List receipts types
export const ReceiptListItemSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  warehouseName: z.string().nullable().optional(),
  status: ReceiptStatusSchema,
  reason: ReceiptReasonSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string(),
  receivedAt: z.string().nullable().optional(),
  approvedAt: z.string().nullable().optional(),
  linesCount: z.number(),
  totalQty: z.number(),
  createdAt: z.string(),
})
export type ReceiptListItem = z.infer<typeof ReceiptListItemSchema>

export const ReceiptsListResultSchema = z.object({
  items: z.array(ReceiptListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type ReceiptsListResult = z.infer<typeof ReceiptsListResultSchema>

export interface ReceiptsListFilters {
  warehouseId?: string
  status?: number
  reason?: number
  fromDate?: string
  toDate?: string
  search?: string
  page?: number
  pageSize?: number
}
