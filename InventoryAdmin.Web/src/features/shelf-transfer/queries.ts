import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'
import type { StockItemsListFilters } from '../stock-items/api'

export function useAssignedStockItems(filters: StockItemsListFilters = {}) {
  return useQuery({
    queryKey: ['shelf-transfer', 'assigned-stock', filters],
    queryFn: () => api.getAssignedStockItems(filters),
    staleTime: 30 * 1000, // 30 seconds
  })
}

export function useMoveStockBetweenShelves() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: api.MoveStockBetweenShelvesDto) => api.moveStockBetweenShelves(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['shelf-transfer'] })
      qc.invalidateQueries({ queryKey: ['stock-items'] })
      qc.invalidateQueries({ queryKey: ['put-away'] })
      toast.success('کالا با موفقیت بین قفسه‌ها منتقل شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در انتقال کالا بین قفسه‌ها')
    },
  })
}

