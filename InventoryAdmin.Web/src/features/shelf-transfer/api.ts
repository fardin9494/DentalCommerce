import { fetchJson, toQuery } from '@/lib/api/client'
import { StockItemsListResultSchema, StockItemsListResult, StockItemsListFilters } from '../stock-items/api'

export interface MoveStockBetweenShelvesDto {
  sourceStockItemId: string
  targetShelfId: string
  qty: number
  note?: string
  serials?: string[]
}

export async function getAssignedStockItems(filters: StockItemsListFilters = {}): Promise<StockItemsListResult> {
  const query = toQuery({ ...filters, shelvedOnly: true })
  const data = await fetchJson<unknown>(`/stock-items${query}`)
  return StockItemsListResultSchema.parse(data)
}

export async function moveStockBetweenShelves(dto: MoveStockBetweenShelvesDto): Promise<void> {
  return fetchJson<void>('/operations/move-stock', { method: 'POST', json: dto })
}
