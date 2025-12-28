import { z } from 'zod'

export const TransferStatusSchema = z.enum(['Draft', 'Shipped', 'PartiallyReceived', 'Completed', 'Canceled'])
export type TransferStatus = z.infer<typeof TransferStatusSchema>

// Status number mappings (for API)
export const TransferStatusMap: Record<TransferStatus, number> = {
  Draft: 1,
  Shipped: 2,
  PartiallyReceived: 3,
  Completed: 4,
  Canceled: 5,
}

// Persian labels
export const TransferStatusLabels: Record<TransferStatus, string> = {
  Draft: 'پیش‌نویس',
  Shipped: 'ارسال شده',
  PartiallyReceived: 'دریافت جزئی',
  Completed: 'تکمیل شده',
  Canceled: 'لغو شده',
}

// Status colors for badges
export const TransferStatusColors: Record<TransferStatus, string> = {
  Draft: 'bg-yellow-100 text-yellow-800',
  Shipped: 'bg-blue-100 text-blue-800',
  PartiallyReceived: 'bg-orange-100 text-orange-800',
  Completed: 'bg-green-100 text-green-800',
  Canceled: 'bg-red-100 text-red-800',
}

export const TransferSegmentSerialSchema = z.object({
  serialNumber: z.string(),
  status: z.string(),
})
export type TransferSegmentSerial = z.infer<typeof TransferSegmentSerialSchema>

export const TransferLineSerialOptionSchema = z.object({
  serialNumber: z.string(),
  stockItemId: z.string().uuid(),
  sku: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  shelfId: z.string().uuid().nullable().optional(),
  shelfName: z.string().nullable().optional(),
})
export type TransferLineSerialOption = z.infer<typeof TransferLineSerialOptionSchema>

export const TransferSegmentSchema = z.object({
  id: z.string().uuid(),
  stockItemId: z.string().uuid(),
  sku: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  shelfName: z.string().nullable().optional(),
  qty: z.number(),
  receivedQty: z.number(),
  remainingToReceive: z.number(),
  serials: z.array(TransferSegmentSerialSchema),
})
export type TransferSegment = z.infer<typeof TransferSegmentSchema>

export const TransferLineSchema = z.object({
  id: z.string().uuid(),
  lineNo: z.number(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  requestedQty: z.number(),
  allocatedQty: z.number(),
  remainingQty: z.number(),
  segments: z.array(TransferSegmentSchema),
})
export type TransferLine = z.infer<typeof TransferLineSchema>

export const TransferDetailSchema = z.object({
  id: z.string().uuid(),
  sourceWarehouseId: z.string().uuid(),
  destinationWarehouseId: z.string().uuid(),
  externalRef: z.string().nullable().optional(),
  docDate: z.string(),
  status: TransferStatusSchema,
  shippedAt: z.string().nullable().optional(),
  completedAt: z.string().nullable().optional(),
  lines: z.array(TransferLineSchema),
})
export type TransferDetail = z.infer<typeof TransferDetailSchema>

export const TransferListItemSchema = z.object({
  id: z.string().uuid(),
  sourceWarehouseId: z.string().uuid(),
  sourceWarehouseName: z.string().nullable().optional(),
  destinationWarehouseId: z.string().uuid(),
  destinationWarehouseName: z.string().nullable().optional(),
  status: TransferStatusSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string(),
  shippedAt: z.string().nullable().optional(),
  completedAt: z.string().nullable().optional(),
  linesCount: z.number(),
  totalQty: z.number(),
  totalAllocatedQty: z.number(),
  totalRemainingQty: z.number(),
  createdAt: z.string(),
})
export type TransferListItem = z.infer<typeof TransferListItemSchema>

export const CreateTransferSchema = z.object({
  sourceWarehouseId: z.string().uuid(),
  destinationWarehouseId: z.string().uuid(),
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type CreateTransferDto = z.infer<typeof CreateTransferSchema>

export const AddTransferLineSchema = z.object({
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  qty: z.number().positive(),
})
export type AddTransferLineDto = z.infer<typeof AddTransferLineSchema>

export const UpdateTransferHeaderSchema = z.object({
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(),
})
export type UpdateTransferHeaderDto = z.infer<typeof UpdateTransferHeaderSchema>

export const UpdateTransferLineSchema = z.object({
  qty: z.number().positive().optional(),
})
export type UpdateTransferLineDto = z.infer<typeof UpdateTransferLineSchema>

export const ReceiveTransferSchema = z.object({
  segmentId: z.string().uuid(),
  qty: z.number().positive(),
})
export type ReceiveTransferDto = z.infer<typeof ReceiveTransferSchema>

export interface TransfersListFilters {
  sourceWarehouseId?: string
  destinationWarehouseId?: string
  status?: number
  fromDate?: string
  toDate?: string
  search?: string
  page?: number
  pageSize?: number
}

