import { fetchJson } from '@/lib/api/client'
import { ReceiptDetailSchema, CreateReceiptSchema, AddReceiptLineSchema, UpdateReceiptHeaderSchema, UpdateReceiptLineSchema, type ReceiptDetail, type CreateReceiptDto, type AddReceiptLineDto, type UpdateReceiptHeaderDto, type UpdateReceiptLineDto } from './types'

export async function getReceipt(id: string): Promise<ReceiptDetail> {
  const data = await fetchJson<unknown>(`/receipts/${id}`)
  return ReceiptDetailSchema.parse(data)
}

export async function createReceipt(dto: CreateReceiptDto): Promise<{ id: string }> {
  const payload = CreateReceiptSchema.parse(dto)
  return fetchJson<{ id: string }>(`/receipts`, { method: 'POST', json: payload })
}

export async function addReceiptLine(receiptId: string, dto: AddReceiptLineDto): Promise<{ id: string }> {
  const payload = AddReceiptLineSchema.parse(dto)
  return fetchJson<{ id: string }>(`/receipts/${receiptId}/lines`, { method: 'POST', json: payload })
}

export async function removeReceiptLine(receiptId: string, lineId: string): Promise<void> {
  return fetchJson<void>(`/receipts/${receiptId}/lines/${lineId}`, { method: 'DELETE' })
}

export async function updateReceiptHeader(receiptId: string, dto: UpdateReceiptHeaderDto): Promise<void> {
  const payload = UpdateReceiptHeaderSchema.parse(dto)
  return fetchJson<void>(`/receipts/${receiptId}`, { method: 'PUT', json: payload })
}

export async function updateReceiptLine(receiptId: string, lineId: string, dto: UpdateReceiptLineDto): Promise<void> {
  const payload = UpdateReceiptLineSchema.parse(dto)
  return fetchJson<void>(`/receipts/${receiptId}/lines/${lineId}`, { method: 'PUT', json: payload })
}

export async function receiveReceipt(receiptId: string, whenUtc?: string): Promise<void> {
  return fetchJson<void>(`/receipts/${receiptId}/receive`, { method: 'POST', json: whenUtc || null })
}

export async function approveReceipt(receiptId: string): Promise<void> {
  return fetchJson<void>(`/receipts/${receiptId}/approve`, { method: 'POST' })
}

export async function cancelReceipt(receiptId: string): Promise<void> {
  return fetchJson<void>(`/receipts/${receiptId}/cancel`, { method: 'POST' })
}


