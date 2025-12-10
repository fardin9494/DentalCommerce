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
      // نمایش پیام خطای مناسب
      // err ممکن است ApiError باشد که از fetchJson throw شده
      const errorMessage = err.details?.detail || err.details?.title || err.message || 'خطا در انتقال کالا به قفسه'
      if (errorMessage.includes('تایید نهایی') || errorMessage.includes('رسید مربوطه')) {
        toast.error(errorMessage)
      } else {
        toast.error(errorMessage)
      }
    },
  })
}

