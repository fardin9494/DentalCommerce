import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'

export const StockItemSchema = z.object({
  id: z.string().uuid(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  warehouseId: z.string().uuid(),
  warehouseName: z.string().nullable().optional(),
  sku: z.string(),
  productName: z.string().nullable().optional(),
  variantValue: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  onHand: z.number(),
  reserved: z.number(),
  blocked: z.number(),
  available: z.number(),
  blockReason: z.string().nullable().optional(),
  shelfId: z.string().uuid().nullable().optional(),
  shelfName: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
})
export type StockItem = z.infer<typeof StockItemSchema>

export const StockItemsListResultSchema = z.object({
  items: z.array(StockItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type StockItemsListResult = z.infer<typeof StockItemsListResultSchema>

export interface StockItemsListFilters {
  warehouseId?: string
  productId?: string
  variantId?: string
  search?: string
  hasStock?: boolean
  page?: number
  pageSize?: number
}

export async function getStockItems(filters: StockItemsListFilters = {}): Promise<StockItemsListResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/stock-items${query}`)
  return StockItemsListResultSchema.parse(data)
}

// Stock Products (for adjustments - only products that exist in stock)
export const StockProductSchema = z.object({
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  sku: z.string(),
  productName: z.string().nullable().optional(),
  variantValue: z.string().nullable().optional(),
  totalOnHand: z.number(),
  totalAvailable: z.number(),
})
export type StockProduct = z.infer<typeof StockProductSchema>

export const StockProductsResultSchema = z.object({
  items: z.array(StockProductSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type StockProductsResult = z.infer<typeof StockProductsResultSchema>

export interface StockProductsFilters {
  warehouseId: string
  search?: string
  page?: number
  pageSize?: number
}

export async function getStockProducts(filters: StockProductsFilters): Promise<StockProductsResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/stock-items/products${query}`)
  return StockProductsResultSchema.parse(data)
}

