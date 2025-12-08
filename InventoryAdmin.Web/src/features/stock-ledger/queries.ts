import { useQuery } from '@tanstack/react-query'
import * as api from './api'
import type { StockLedgerFilters } from './types'

export function useStockLedger(filters: StockLedgerFilters = {}) {
  return useQuery({
    queryKey: ['stock-ledger', 'list', filters],
    queryFn: () => api.getStockLedger(filters),
    staleTime: 1 * 60 * 1000, // 1 minute
  })
}

export function useStockLedgerEntryDetails(id?: string) {
  return useQuery({
    queryKey: ['stock-ledger', 'detail', id],
    queryFn: () => api.getStockLedgerEntryDetails(id!),
    enabled: !!id,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

