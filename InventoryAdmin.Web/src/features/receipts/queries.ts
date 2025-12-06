import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'

export function useReceipt(id?: string) {
  return useQuery({
    queryKey: ['receipts', 'detail', id],
    queryFn: () => api.getReceipt(id!),
    enabled: !!id,
  })
}

export function useCreateReceipt() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createReceipt,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['receipts'] })
      toast.success('رسید با موفقیت ایجاد شد')
      return data
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد رسید')
    },
  })
}

export function useAddReceiptLine(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: Parameters<typeof api.addReceiptLine>[1]) => api.addReceiptLine(receiptId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('خط با موفقیت اضافه شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در اضافه کردن خط')
    },
  })
}

export function useRemoveReceiptLine(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.removeReceiptLine(receiptId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('خط با موفقیت حذف شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در حذف خط')
    },
  })
}

export function useUpdateReceiptHeader(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: Parameters<typeof api.updateReceiptHeader>[1]) => api.updateReceiptHeader(receiptId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('هدر با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی هدر')
    },
  })
}

export function useUpdateReceiptLine(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, dto }: { lineId: string; dto: Parameters<typeof api.updateReceiptLine>[2] }) =>
      api.updateReceiptLine(receiptId, lineId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('خط با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی خط')
    },
  })
}

export function useReceiveReceipt(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (whenUtc?: string) => api.receiveReceipt(receiptId, whenUtc),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('رسید با موفقیت دریافت شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در دریافت رسید')
    },
  })
}

export function useApproveReceipt(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.approveReceipt(receiptId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('رسید با موفقیت تایید شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تایید رسید')
    },
  })
}

export function useCancelReceipt(receiptId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.cancelReceipt(receiptId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['receipts', 'detail', receiptId] })
      toast.success('رسید با موفقیت لغو شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در لغو رسید')
    },
  })
}


