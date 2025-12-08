import { useQuery } from '@tanstack/react-query'
import { getStockItems, getStockProducts, type StockItemsListFilters, type StockProductsFilters } from './api'

export function useStockItems(filters: StockItemsListFilters = {}) {
  return useQuery({
    queryKey: ['stock-items', 'list', filters],
    queryFn: () => getStockItems(filters),
  })
}

export function useStockProducts(filters: StockProductsFilters) {
  return useQuery({
    queryKey: ['stock-items', 'products', filters],
    queryFn: () => getStockProducts(filters),
    enabled: !!filters.warehouseId,
  })
}

