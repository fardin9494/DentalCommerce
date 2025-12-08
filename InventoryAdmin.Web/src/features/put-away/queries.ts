import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'

export function useUnassignedStockItems(filters: api.UnassignedStockItemsFilters = {}) {
  return useQuery({
    queryKey: ['stock-items', 'unassigned', filters],
    queryFn: () => api.getUnassignedStockItems(filters),
  })
}

export function useMoveStockToShelf() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.moveStockToShelf,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['stock-items'] })
      qc.invalidateQueries({ queryKey: ['stock-items', 'unassigned'] })
      toast.success('کالا با موفقیت به قفسه منتقل شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در انتقال کالا به قفسه')
    },
  })
}

