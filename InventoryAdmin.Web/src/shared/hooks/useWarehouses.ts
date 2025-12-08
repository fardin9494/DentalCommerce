import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useToast } from '../components/toast/ToastProvider'
import * as api from '../api/warehouses'

export function useWarehouses(isActive?: boolean) {
  return useQuery({
    queryKey: ['warehouses', { isActive }],
    queryFn: () => api.getWarehouses(isActive),
    staleTime: 5 * 60 * 1000, // Cache for 5 minutes
  })
}

export function useActiveWarehouses() {
  return useWarehouses(true)
}

export function useAllWarehouses() {
  return useWarehouses(undefined)
}

export function useCreateWarehouse() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createWarehouse,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      toast.success('انبار با موفقیت ایجاد شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد انبار')
    },
  })
}

export function useUpdateWarehouse() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ id, dto }: { id: string; dto: api.UpdateWarehouseDto }) => api.updateWarehouse(id, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      toast.success('انبار با موفقیت به‌روزرسانی شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در به‌روزرسانی انبار')
    },
  })
}

export function useActivateWarehouse() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.activateWarehouse,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      toast.success('انبار فعال شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در فعال‌سازی انبار')
    },
  })
}

export function useDeactivateWarehouse() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.deactivateWarehouse,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['warehouses'] })
      toast.success('انبار غیرفعال شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در غیرفعال‌سازی انبار')
    },
  })
}

