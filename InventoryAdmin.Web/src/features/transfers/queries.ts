import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'
import type {
  AddTransferLineDto,
  TransfersListFilters,
  UpdateTransferHeaderDto,
  UpdateTransferLineDto,
  CreateTransferDto,
  ReceiveTransferDto,
} from './types'

export function useTransfersList(filters: TransfersListFilters = {}) {
  return useQuery({
    queryKey: ['transfers', 'list', filters],
    queryFn: () => api.getTransfersList(filters),
    staleTime: 5 * 60 * 1000,
  })
}

export function useTransfer(id?: string) {
  return useQuery({
    queryKey: ['transfers', 'detail', id],
    queryFn: () => api.getTransfer(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000,
  })
}

export function useCreateTransfer() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: CreateTransferDto) => api.createTransfer(dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      toast.success('سند انتقال با موفقیت ایجاد شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد سند انتقال')
    },
  })
}

export function useAddTransferLine(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: AddTransferLineDto) => api.addTransferLine(transferId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('خط انتقال با موفقیت اضافه شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در افزودن خط انتقال')
    },
  })
}

export function useRemoveTransferLine(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.removeTransferLine(transferId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('خط انتقال با موفقیت حذف شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در حذف خط انتقال')
    },
  })
}

export function useUpdateTransferHeader(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: UpdateTransferHeaderDto) => api.updateTransferHeader(transferId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      toast.success('هدر سند انتقال با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی هدر سند انتقال')
    },
  })
}

export function useUpdateTransferLine(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, dto }: { lineId: string; dto: UpdateTransferLineDto }) =>
      api.updateTransferLine(transferId, lineId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('خط انتقال با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی خط انتقال')
    },
  })
}

export function useAllocateTransferLineFefo(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.allocateTransferLineFefo(transferId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('تخصیص موجودی (FEFO) با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص موجودی (FEFO)')
    },
  })
}

export function useAllocateTransferLineFifo(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.allocateTransferLineFifo(transferId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('تخصیص موجودی (FIFO) با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص موجودی (FIFO)')
    },
  })
}

export function useAllocateTransferLineLifo(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.allocateTransferLineLifo(transferId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('تخصیص موجودی (LIFO) با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص موجودی (LIFO)')
    },
  })
}

export function useTransferLineAvailableSerials(transferId: string, lineId: string, enabled: boolean = true) {
  return useQuery({
    queryKey: ['transfers', 'line-serials', transferId, lineId],
    queryFn: () => api.getTransferLineAvailableSerials(transferId, lineId),
    enabled: enabled && !!transferId && !!lineId,
  })
}

export function useAllocateTransferLineSerials(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, serials }: { lineId: string; serials: string[] }) =>
      api.allocateTransferLineSerials(transferId, lineId, serials),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      toast.success('تخصیص سریال با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص سریال')
    },
  })
}

export function useShipTransfer(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.shipTransfer(transferId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      qc.invalidateQueries({ queryKey: ['stock-items'] })
      toast.success('سند انتقال با موفقیت ارسال شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ارسال سند انتقال')
    },
  })
}

export function useReceiveTransfer(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: ReceiveTransferDto) => api.receiveTransfer(transferId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      qc.invalidateQueries({ queryKey: ['stock-items'] })
      toast.success('دریافت انتقال با موفقیت ثبت شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ثبت دریافت انتقال')
    },
  })
}

export function useCompleteTransfer(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.completeTransfer(transferId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      toast.success('انتقال با موفقیت تایید شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تایید نهایی انتقال')
    },
  })
}

export function useCancelTransfer(transferId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.cancelTransfer(transferId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['transfers', 'detail', transferId] })
      qc.invalidateQueries({ queryKey: ['transfers', 'list'] })
      toast.success('سند انتقال با موفقیت لغو شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در لغو سند انتقال')
    },
  })
}

