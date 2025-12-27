import { fetchJson, toQuery } from '@/lib/api/client'
import { ReceiptRejectionsListResultSchema, type ReceiptRejectionsListFilters, type ReceiptRejectionsListResult } from './types'

export async function getReceiptRejectionsList(filters: ReceiptRejectionsListFilters = {}): Promise<ReceiptRejectionsListResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/receipt-rejections${query}`)
  return ReceiptRejectionsListResultSchema.parse(data)
}

export async function resolveReceiptRejection(
  receiptLineId: string,
  body: { approvedQty: number; returnedQty: number; disposedQty: number; note?: string }
): Promise<void> {
  return fetchJson<void>(`/receipt-rejections/${receiptLineId}/resolve`, {
    method: 'POST',
    json: {
      approvedQty: body.approvedQty,
      returnedQty: body.returnedQty,
      disposedQty: body.disposedQty,
      note: body.note || null,
    },
  })
}
