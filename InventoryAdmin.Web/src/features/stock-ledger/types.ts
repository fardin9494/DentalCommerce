import { z } from 'zod'

export const StockMovementTypeSchema = z.enum([
  'Receipt',
  'Issue',
  'TransferOut',
  'TransferIn',
  'AdjustmentPlus',
  'AdjustmentMinus',
  'ShelfTransferOut',
  'ShelfTransferIn',
])
export type StockMovementType = z.infer<typeof StockMovementTypeSchema>

export const StockMovementTypeLabels: Record<StockMovementType, string> = {
  Receipt: 'ورود',
  Issue: 'خروج',
  TransferOut: 'انتقال (خروج)',
  TransferIn: 'انتقال (ورود)',
  AdjustmentPlus: 'اصلاح (افزایش)',
  AdjustmentMinus: 'اصلاح (کاهش)',
  ShelfTransferOut: 'انتقال قفسه (خروج)',
  ShelfTransferIn: 'انتقال قفسه (ورود)',
}

export const StockMovementTypeColors: Record<StockMovementType, string> = {
  Receipt: 'bg-emerald-100 text-emerald-800',
  Issue: 'bg-red-100 text-red-800',
  TransferOut: 'bg-blue-100 text-blue-800',
  TransferIn: 'bg-blue-100 text-blue-800',
  AdjustmentPlus: 'bg-green-100 text-green-800',
  AdjustmentMinus: 'bg-orange-100 text-orange-800',
  ShelfTransferOut: 'bg-purple-100 text-purple-800',
  ShelfTransferIn: 'bg-purple-100 text-purple-800',
}

export const StockLedgerEntrySchema = z.object({
  id: z.string().uuid(),
  timestamp: z.string(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  warehouseId: z.string().uuid(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  deltaQty: z.number(),
  unitCost: z.number().nullable().optional(),
  movementType: StockMovementTypeSchema,
  refDocType: z.string(),
  refDocId: z.string().uuid(),
  note: z.string().nullable().optional(),
})
export type StockLedgerEntry = z.infer<typeof StockLedgerEntrySchema>

export const StockLedgerListResultSchema = z.object({
  items: z.array(StockLedgerEntrySchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type StockLedgerListResult = z.infer<typeof StockLedgerListResultSchema>

export type StockLedgerSortField = 'timestamp' | 'movementType' | 'deltaQty' | 'refDocType' | 'lotNumber' | 'expiryDate'

export interface StockLedgerFilters {
  warehouseId?: string
  productId?: string
  variantId?: string
  movementType?: number
  refDocType?: string
  refDocId?: string
  fromDate?: string
  toDate?: string
  page?: number
  pageSize?: number
  sortBy?: StockLedgerSortField
  sortDirection?: 'asc' | 'desc'
}

export const StockLedgerEntryDetailsSchema = z.object({
  id: z.string().uuid(),
  timestamp: z.string(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  warehouseId: z.string().uuid(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  deltaQty: z.number(),
  unitCost: z.number().nullable().optional(),
  movementType: StockMovementTypeSchema,
  refDocType: z.string(),
  refDocId: z.string().uuid(),
  note: z.string().nullable().optional(),
  warehouseName: z.string().nullable().optional(),
  productName: z.string().nullable().optional(),
  variantName: z.string().nullable().optional(),
  sku: z.string().nullable().optional(),
  refDocDetails: z.any().nullable().optional(),
})
export type StockLedgerEntryDetails = z.infer<typeof StockLedgerEntryDetailsSchema>

