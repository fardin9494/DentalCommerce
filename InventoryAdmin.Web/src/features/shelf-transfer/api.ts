import { fetchJson, toQuery } from '@/lib/api/client'
import { StockItemsListResultSchema, StockItemsListResult, StockItemsListFilters } from '../stock-items/api'

export interface MoveStockBetweenShelvesDto {
  sourceStockItemId: string
  targetShelfId: string
  qty: number
  note?: string
}

export async function getAssignedStockItems(filters: StockItemsListFilters = {}): Promise<StockItemsListResult> {
  // Use the same endpoint but filter for items with shelfId
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/stock-items${query}`)
  const result = StockItemsListResultSchema.parse(data)
  
  // Filter to only show items that have a shelf assigned
  const filteredItems = result.items.filter(item => item.shelfId != null)
  
  return {
    ...result,
    items: filteredItems,
    totalCount: filteredItems.length,
    totalPages: Math.ceil(filteredItems.length / (result.pageSize || 20)),
  }
}

export async function moveStockBetweenShelves(dto: MoveStockBetweenShelvesDto): Promise<void> {
  return fetchJson<void>('/operations/move-stock', { method: 'POST', json: dto })
}

