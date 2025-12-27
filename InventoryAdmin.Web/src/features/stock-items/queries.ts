import { useQuery } from '@tanstack/react-query'
import { getStockItems, getStockProducts, getStockItemSerials, type StockItemsListFilters, type StockProductsFilters } from './api'

export function useStockItems(filters: StockItemsListFilters = {}, enabled: boolean = true) {
  return useQuery({
    queryKey: ['stock-items', 'list', filters],
    queryFn: () => getStockItems(filters),
    enabled,
  })
}

export function useStockProducts(filters: StockProductsFilters) {
  return useQuery({
    queryKey: ['stock-items', 'products', filters],
    queryFn: () => getStockProducts(filters),
    enabled: !!filters.warehouseId,
  })
}

export function useStockItemSerials(stockItemId?: string, status?: string, enabled: boolean = true) {
  return useQuery({
    queryKey: ['stock-items', 'serials', stockItemId, status],
    queryFn: () => getStockItemSerials(stockItemId!, status),
    enabled: enabled && !!stockItemId,
  })
}

