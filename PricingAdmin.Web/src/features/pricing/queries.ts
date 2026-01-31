import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as api from './api'

export function usePricingPolicy(siteId?: string | null) {
  const key = siteId ? siteId : 'global'
  return useQuery({
    queryKey: ['pricing', 'policy', key],
    queryFn: () => api.getPricingPolicy(siteId),
  })
}

export function usePricingPolicies() {
  return useQuery({
    queryKey: ['pricing', 'policies'],
    queryFn: api.listPricingPolicies,
  })
}

export function useUpsertPricingPolicy() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.upsertPricingPolicy,
    onSuccess: (_res, vars) => {
      const key = vars.siteId ? vars.siteId : 'global'
      qc.invalidateQueries({ queryKey: ['pricing', 'policy', key] })
      qc.invalidateQueries({ queryKey: ['pricing', 'policies'] })
    },
  })
}

export function usePriceLists() {
  return useQuery({
    queryKey: ['pricing', 'pricelists'],
    queryFn: api.listPriceLists,
  })
}

export function usePriceList(id?: string) {
  return useQuery({
    queryKey: ['pricing', 'pricelist', id],
    queryFn: () => api.getPriceList(id!),
    enabled: !!id,
  })
}

export function useCreatePriceList() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createPriceList,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'pricelists'] }),
  })
}

export function useUpdatePriceList(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: { name: string; currency: string; validFrom?: string | null; validTo?: string | null; isActive: boolean }) =>
      api.updatePriceList(id, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['pricing', 'pricelists'] })
      qc.invalidateQueries({ queryKey: ['pricing', 'pricelist', id] })
    },
  })
}

export function useDeletePriceList() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.deletePriceList,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'pricelists'] }),
  })
}

export function useBulkUpdatePrices(id?: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.bulkUpdatePrices,
    onSuccess: () => {
      if (id) qc.invalidateQueries({ queryKey: ['pricing', 'pricelist', id] })
      qc.invalidateQueries({ queryKey: ['pricing', 'pricelists'] })
    },
  })
}

export function useOverrides(params: { scopeType?: string; scopeId?: string; skuId?: string }) {
  return useQuery({
    queryKey: ['pricing', 'overrides', params],
    queryFn: () => api.listOverrides(params as any),
  })
}

export function useCreateOverride() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createOverride,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'overrides'] }),
  })
}

export function useUpdateOverride(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: {
      overrideType: any
      value: number
      currency: string
      validFrom?: string | null
      validTo?: string | null
      priority: number
      stackingGroup: string
    }) => api.updateOverride(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'overrides'] }),
  })
}

export function useDeleteOverride() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.deleteOverride,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'overrides'] }),
  })
}

export function useCampaigns(isActive?: boolean) {
  return useQuery({
    queryKey: ['pricing', 'campaigns', isActive ?? 'all'],
    queryFn: () => api.listCampaigns({ isActive }),
  })
}

export function useCreateCampaign() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createCampaign,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'campaigns'] }),
  })
}

export function useUpdateCampaign(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: any) => api.updateCampaign(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'campaigns'] }),
  })
}

export function useDeleteCampaign() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.deleteCampaign,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'campaigns'] }),
  })
}

export function useCoupons(params: { isActive?: boolean; code?: string }) {
  return useQuery({
    queryKey: ['pricing', 'coupons', params],
    queryFn: () => api.listCoupons(params),
  })
}

export function useCouponUsage(id?: string, userId?: string) {
  return useQuery({
    queryKey: ['pricing', 'coupon-usage', id ?? 'none', userId ?? 'none'],
    queryFn: () => api.getCouponUsage(id!, userId),
    enabled: !!id,
  })
}

export function useCouponReservations(id?: string, activeOnly = true) {
  return useQuery({
    queryKey: ['pricing', 'coupon-reservations', id ?? 'none', activeOnly ? 'active' : 'all'],
    queryFn: () => api.listCouponReservations(id!, activeOnly),
    enabled: !!id,
  })
}

export function useCreateCoupon() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.createCoupon,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'coupons'] }),
  })
}

export function useUpdateCoupon(id: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (input: any) => api.updateCoupon(id, input),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'coupons'] }),
  })
}

export function useDeleteCoupon() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: api.deleteCoupon,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['pricing', 'coupons'] }),
  })
}

export function useBulkCreateCategoryCampaign() {
  return useMutation({
    mutationFn: api.bulkCreateCategoryCampaign,
  })
}
