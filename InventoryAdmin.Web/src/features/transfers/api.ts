import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'
import {
  TransferDetailSchema,
  TransferListItemSchema,
  CreateTransferSchema,
  AddTransferLineSchema,
  UpdateTransferHeaderSchema,
  UpdateTransferLineSchema,
  ReceiveTransferSchema,
  TransferLineSerialOptionSchema,
  type TransferDetail,
  type TransfersListFilters,
  type CreateTransferDto,
  type AddTransferLineDto,
  type UpdateTransferHeaderDto,
  type UpdateTransferLineDto,
  type ReceiveTransferDto,
  type TransferLineSerialOption,
} from './types'

export const TransfersListResultSchema = z.object({
  items: z.array(TransferListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type TransfersListResult = z.infer<typeof TransfersListResultSchema>

export async function getTransfersList(filters: TransfersListFilters = {}): Promise<TransfersListResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/transfers${query}`)
  return TransfersListResultSchema.parse(data)
}

export async function getTransfer(id: string): Promise<TransferDetail> {
  const data = await fetchJson<unknown>(`/transfers/${id}`)
  return TransferDetailSchema.parse(data)
}

export async function createTransfer(dto: CreateTransferDto): Promise<{ id: string }> {
  const payload = CreateTransferSchema.parse(dto)
  return fetchJson<{ id: string }>(`/transfers`, { method: 'POST', json: payload })
}

export async function addTransferLine(transferId: string, dto: AddTransferLineDto): Promise<{ id: string }> {
  const payload = AddTransferLineSchema.parse(dto)
  return fetchJson<{ id: string }>(`/transfers/${transferId}/lines`, { method: 'POST', json: payload })
}

export async function removeTransferLine(transferId: string, lineId: string): Promise<void> {
  return fetchJson<void>(`/transfers/${transferId}/lines/${lineId}`, { method: 'DELETE' })
}

export async function updateTransferHeader(transferId: string, dto: UpdateTransferHeaderDto): Promise<void> {
  const payload = UpdateTransferHeaderSchema.parse(dto)
  return fetchJson<void>(`/transfers/${transferId}`, { method: 'PUT', json: payload })
}

export async function updateTransferLine(
  transferId: string,
  lineId: string,
  dto: UpdateTransferLineDto
): Promise<void> {
  const payload = UpdateTransferLineSchema.parse(dto)
  return fetchJson<void>(`/transfers/${transferId}/lines/${lineId}`, { method: 'PUT', json: payload })
}

export async function allocateTransferLineFefo(transferId: string, lineId: string): Promise<Array<{ stockItemId: string; qty: number }>> {
  return fetchJson<Array<{ stockItemId: string; qty: number }>>(`/transfers/${transferId}/lines/${lineId}/allocate-fefo`, { method: 'POST' })
}

export async function allocateTransferLineFifo(transferId: string, lineId: string): Promise<Array<{ stockItemId: string; qty: number }>> {
  return fetchJson<Array<{ stockItemId: string; qty: number }>>(`/transfers/${transferId}/lines/${lineId}/allocate-fifo`, { method: 'POST' })
}

export async function allocateTransferLineLifo(transferId: string, lineId: string): Promise<Array<{ stockItemId: string; qty: number }>> {
  return fetchJson<Array<{ stockItemId: string; qty: number }>>(`/transfers/${transferId}/lines/${lineId}/allocate-lifo`, { method: 'POST' })
}

export async function getTransferLineAvailableSerials(transferId: string, lineId: string): Promise<TransferLineSerialOption[]> {
  const data = await fetchJson<unknown>(`/transfers/${transferId}/lines/${lineId}/available-serials`)
  return TransferLineSerialOptionSchema.array().parse(data)
}

export async function allocateTransferLineSerials(transferId: string, lineId: string, serials: string[]): Promise<void> {
  return fetchJson<void>(`/transfers/${transferId}/lines/${lineId}/allocate-serials`, { method: 'POST', json: { serials } })
}

export async function shipTransfer(transferId: string): Promise<void> {
  return fetchJson<void>(`/transfers/${transferId}/ship`, { method: 'POST' })
}

export async function receiveTransfer(transferId: string, dto: ReceiveTransferDto): Promise<void> {
  const payload = ReceiveTransferSchema.parse(dto)
  return fetchJson<void>(`/transfers/${transferId}/receive`, { method: 'POST', json: payload })
}

export async function completeTransfer(transferId: string): Promise<void> {
  return fetchJson<void>(`/transfers/${transferId}/complete`, { method: 'POST' })
}

export async function cancelTransfer(transferId: string): Promise<void> {
  return fetchJson<void>(`/transfers/${transferId}/cancel`, { method: 'POST' })
}

