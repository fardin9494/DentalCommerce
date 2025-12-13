import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'

export function useShelves(warehouseId?: string, isActive?: boolean, enabled: boolean = true) {
  return useQuery({
    queryKey: ['shelves', { warehouseId, isActive }],
    queryFn: () => api.getShelves(warehouseId, isActive),
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
    enabled,
  })
}

export function useAllShelves() {
  return useShelves(undefined, undefined)
}

export function useActiveShelves(warehouseId?: string) {
  return useShelves(warehouseId, true)
}

export function useCreateShelf() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createShelf,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['shelves'] })
      toast.success('قفسه با موفقیت ایجاد شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد قفسه')
    },
  })
}

export function useCreateShelvesBatch() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createShelvesBatch,
    onSuccess: (res) => {
      qc.invalidateQueries({ queryKey: ['shelves'] })
      toast.success(`قفسه‌ها با موفقیت ایجاد شدند (تعداد: ${res.created})`)
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد گروهی قفسه‌ها')
    },
  })
}

export function useUpdateShelf() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: api.UpdateShelfDto }) => api.updateShelf(id, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['shelves'] })
      toast.success('قفسه با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی قفسه')
    },
  })
}

export function useActivateShelf() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.activateShelf,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['shelves'] })
      toast.success('قفسه فعال شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در فعال‌سازی قفسه')
    },
  })
}

export function useDeactivateShelf() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.deactivateShelf,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['shelves'] })
      toast.success('قفسه غیرفعال شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در غیرفعال‌سازی قفسه')
    },
  })
}

