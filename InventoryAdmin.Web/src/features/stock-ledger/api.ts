import { toQuery } from '@/lib/api/client'
import { fetchJson } from '@/lib/api/client'
import { StockLedgerListResultSchema, StockLedgerEntryDetailsSchema, type StockLedgerFilters, type StockLedgerEntryDetails } from './types'

export async function getStockLedger(filters: StockLedgerFilters = {}): Promise<StockLedgerListResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/stock-ledger${query}`)
  return StockLedgerListResultSchema.parse(data)
}

export async function getStockLedgerEntryDetails(id: string): Promise<StockLedgerEntryDetails> {
  const data = await fetchJson<unknown>(`/stock-ledger/${id}`)
  return StockLedgerEntryDetailsSchema.parse(data)
}

