import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'

export const UnassignedStockItemSchema = z.object({
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
})
export type UnassignedStockItem = z.infer<typeof UnassignedStockItemSchema>

export const UnassignedStockItemsResultSchema = z.object({
  items: z.array(UnassignedStockItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type UnassignedStockItemsResult = z.infer<typeof UnassignedStockItemsResultSchema>

export interface UnassignedStockItemsFilters {
  warehouseId?: string
  productId?: string
  search?: string
  page?: number
  pageSize?: number
}

export interface MoveToShelfDto {
  sourceStockItemId: string
  targetShelfId: string
  qty: number
  note?: string
}

export async function getUnassignedStockItems(filters: UnassignedStockItemsFilters = {}): Promise<UnassignedStockItemsResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/stock-items/unassigned${query}`)
  return UnassignedStockItemsResultSchema.parse(data)
}

export async function moveStockToShelf(dto: MoveToShelfDto): Promise<void> {
  return fetchJson<void>('/operations/move-stock', { method: 'POST', json: dto })
}

