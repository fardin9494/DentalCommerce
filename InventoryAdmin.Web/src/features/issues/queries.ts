import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useToast } from '@/shared/components/toast/ToastProvider'
import * as api from './api'
import type { IssuesListFilters } from './types'

export function useIssuesList(filters: IssuesListFilters = {}) {
  return useQuery({
    queryKey: ['issues', 'list', filters],
    queryFn: () => api.getIssuesList(filters),
  })
}

export function useIssue(id?: string) {
  return useQuery({
    queryKey: ['issues', 'detail', id],
    queryFn: () => api.getIssue(id!),
    enabled: !!id,
  })
}

export function useCreateIssue() {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: api.createIssue,
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: ['issues'] })
      toast.success('خروجی با موفقیت ایجاد شد')
      return data
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ایجاد خروجی')
    },
  })
}

export function useAddIssueLine(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (dto: Parameters<typeof api.addIssueLine>[1]) => api.addIssueLine(issueId, dto),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('خط با موفقیت اضافه شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در اضافه کردن خط')
    },
  })
}

export function useRemoveIssueLine(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (lineId: string) => api.removeIssueLine(issueId, lineId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('خط با موفقیت حذف شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در حذف خط')
    },
  })
}

export function useAllocateIssueLineFefo(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, preferredWarehouseId }: { lineId: string; preferredWarehouseId?: string }) => 
      api.allocateIssueLineFefo(issueId, lineId, preferredWarehouseId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('تخصیص FEFO با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص FEFO')
    },
  })
}

export function useAllocateIssueLineFifo(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, preferredWarehouseId }: { lineId: string; preferredWarehouseId?: string }) => 
      api.allocateIssueLineFifo(issueId, lineId, preferredWarehouseId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('تخصیص FIFO با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص FIFO')
    },
  })
}

export function useAllocateIssueLineLifo(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, preferredWarehouseId }: { lineId: string; preferredWarehouseId?: string }) => 
      api.allocateIssueLineLifo(issueId, lineId, preferredWarehouseId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('تخصیص LIFO با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص LIFO')
    },
  })
}

export function useIssueLineAvailableSerials(issueId: string, lineId: string, warehouseId?: string, enabled: boolean = true) {
  return useQuery({
    queryKey: ['issues', 'line-serials', issueId, lineId, warehouseId],
    queryFn: () => api.getIssueLineAvailableSerials(issueId, lineId, warehouseId),
    enabled: enabled && !!issueId && !!lineId,
  })
}

export function useAllocateIssueLineSerials(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: ({ lineId, serials }: { lineId: string; serials: string[] }) =>
      api.allocateIssueLineSerials(issueId, lineId, serials),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('تخصیص سریال با موفقیت انجام شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در تخصیص سریال')
    },
  })
}

export function usePostIssue(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: (whenUtc?: string) => api.postIssue(issueId, whenUtc),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('خروجی با موفقیت ثبت شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در ثبت خروجی')
    },
  })
}

export function useCancelIssue(issueId: string) {
  const qc = useQueryClient()
  const toast = useToast()
  return useMutation({
    mutationFn: () => api.cancelIssue(issueId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['issues', 'detail', issueId] })
      toast.success('خروجی با موفقیت لغو شد')
    },
    onError: (err: any) => {
      toast.error(err.message || 'خطا در لغو خروجی')
    },
  })
}



