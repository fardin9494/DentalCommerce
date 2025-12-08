import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'

export function useAdjustmentsList(filters: api.AdjustmentsListFilters = {}) {
  return useQuery({
    queryKey: ['adjustments', 'list', filters],
    queryFn: () => api.getAdjustmentsList(filters),
  })
}

export function useAdjustment(id: string | undefined) {
  return useQuery({
    queryKey: ['adjustments', id],
    queryFn: () => api.getAdjustment(id!),
    enabled: !!id,
  })
}

export function useCreateAdjustment() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createAdjustment,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['adjustments'] })
      toast.success('اصلاح موجودی با موفقیت ایجاد شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد اصلاح موجودی')
    },
  })
}

export function useAddAdjustmentLine() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ adjustmentId, dto }: { adjustmentId: string; dto: api.AddAdjustmentLineDto }) =>
      api.addAdjustmentLine(adjustmentId, dto),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: ['adjustments', variables.adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      toast.success('خط اصلاح با موفقیت اضافه شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در افزودن خط اصلاح')
    },
  })
}

export function useRemoveAdjustmentLine() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ adjustmentId, lineId }: { adjustmentId: string; lineId: string }) =>
      api.removeAdjustmentLine(adjustmentId, lineId),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: ['adjustments', variables.adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      toast.success('خط اصلاح با موفقیت حذف شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در حذف خط اصلاح')
    },
  })
}

export function useUpdateAdjustmentHeader() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ adjustmentId, dto }: { adjustmentId: string; dto: api.UpdateAdjustmentHeaderDto }) =>
      api.updateAdjustmentHeader(adjustmentId, dto),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: ['adjustments', variables.adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      toast.success('هدر اصلاح با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی هدر اصلاح')
    },
  })
}

export function useUpdateAdjustmentLine() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({
      adjustmentId,
      lineId,
      dto,
    }: {
      adjustmentId: string
      lineId: string
      dto: api.UpdateAdjustmentLineDto
    }) => api.updateAdjustmentLine(adjustmentId, lineId, dto),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: ['adjustments', variables.adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      toast.success('خط اصلاح با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی خط اصلاح')
    },
  })
}

export function usePostAdjustment() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.postAdjustment,
    onSuccess: (_, adjustmentId) => {
      qc.invalidateQueries({ queryKey: ['adjustments', adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      qc.invalidateQueries({ queryKey: ['stock-items'] })
      toast.success('اصلاح موجودی با موفقیت ثبت شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ثبت اصلاح موجودی')
    },
  })
}

export function useCancelAdjustment() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.cancelAdjustment,
    onSuccess: (_, adjustmentId) => {
      qc.invalidateQueries({ queryKey: ['adjustments', adjustmentId] })
      qc.invalidateQueries({ queryKey: ['adjustments', 'list'] })
      toast.success('اصلاح موجودی با موفقیت لغو شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در لغو اصلاح موجودی')
    },
  })
}

