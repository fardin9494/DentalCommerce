import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'
import type { ReceiptRejectionsListFilters } from './types'

export function useReceiptRejectionsList(filters: ReceiptRejectionsListFilters = {}) {
  return useQuery({
    queryKey: ['receipt-rejections', 'list', filters],
    queryFn: () => api.getReceiptRejectionsList(filters),
  })
}

export function useResolveReceiptRejection() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (payload: {
      receiptLineId: string
      approvedQty: number
      returnedQty: number
      disposedQty: number
      note?: string
    }) =>
      api.resolveReceiptRejection(payload.receiptLineId, {
        approvedQty: payload.approvedQty,
        returnedQty: payload.returnedQty,
        disposedQty: payload.disposedQty,
        note: payload.note,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipt-rejections'] })
      toast.success('Rejection resolution saved.')
    },
    onError: (err: any) => {
      toast.error(err.message || 'Failed to resolve rejection.')
    },
  })
}
