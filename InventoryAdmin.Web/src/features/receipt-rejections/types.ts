import { z } from 'zod'

export const ReceiptRejectionStatusSchema = z.enum(['None', 'Pending', 'ApprovedToStock', 'Returned', 'Disposed', 'Mixed'])
export type ReceiptRejectionStatus = z.infer<typeof ReceiptRejectionStatusSchema>

export const ReceiptRejectionStatusMap: Record<ReceiptRejectionStatus, number> = {
  None: 0,
  Pending: 1,
  ApprovedToStock: 2,
  Returned: 3,
  Disposed: 4,
  Mixed: 5,
}

export const ReceiptRejectionStatusLabels: Record<ReceiptRejectionStatus, string> = {
  None: 'نامشخص',
  Pending: 'در انتظار رسیدگی',
  ApprovedToStock: 'تایید و افزودن به انبار',
  Returned: 'مرجوعی',
  Disposed: 'معدوم',
  Mixed: 'ترکیبی',
}

export const ReceiptRejectionStatusColors: Record<ReceiptRejectionStatus, string> = {
  None: 'bg-slate-100 text-slate-700',
  Pending: 'bg-amber-100 text-amber-800',
  ApprovedToStock: 'bg-emerald-100 text-emerald-700',
  Returned: 'bg-blue-100 text-blue-700',
  Disposed: 'bg-red-100 text-red-700',
  Mixed: 'bg-slate-200 text-slate-700',
}

export const ReceiptRejectionListItemSchema = z.object({
  receiptLineId: z.string().uuid(),
  receiptId: z.string().uuid(),
  warehouseId: z.string().uuid(),
  warehouseName: z.string().nullable().optional(),
  receiptStatus: z.string(),
  receiptReason: z.string(),
  receiptExternalRef: z.string().nullable().optional(),
  receiptDocDate: z.string(),
  lineNo: z.number(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDateUtc: z.string().nullable().optional(),
  unitCost: z.number().nullable().optional(),
  rejectedQty: z.number(),
  rejectionApprovedQty: z.number(),
  rejectionReturnedQty: z.number(),
  rejectionDisposedQty: z.number(),
  rejectionReason: z.string().nullable().optional(),
  rejectionStatus: ReceiptRejectionStatusSchema,
  rejectionResolvedAt: z.string().nullable().optional(),
  rejectionResolutionNote: z.string().nullable().optional(),
  createdAt: z.string(),
})
export type ReceiptRejectionListItem = z.infer<typeof ReceiptRejectionListItemSchema>

export const ReceiptRejectionsListResultSchema = z.object({
  items: z.array(ReceiptRejectionListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type ReceiptRejectionsListResult = z.infer<typeof ReceiptRejectionsListResultSchema>

export interface ReceiptRejectionsListFilters {
  warehouseId?: string
  status?: number
  fromDate?: string
  toDate?: string
  search?: string
  page?: number
  pageSize?: number
}
