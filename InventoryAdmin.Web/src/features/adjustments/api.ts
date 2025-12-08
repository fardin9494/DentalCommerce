import { fetchJson, toQuery } from '@/lib/api/client'
import {
  AdjustmentDetailSchema,
  CreateAdjustmentSchema,
  AddAdjustmentLineSchema,
  UpdateAdjustmentHeaderSchema,
  UpdateAdjustmentLineSchema,
  AdjustmentsListResultSchema,
  type AdjustmentDetail,
  type CreateAdjustmentDto,
  type AddAdjustmentLineDto,
  type UpdateAdjustmentHeaderDto,
  type UpdateAdjustmentLineDto,
  type AdjustmentsListResult,
  type AdjustmentsListFilters,
} from './types'

export async function getAdjustmentsList(filters: AdjustmentsListFilters = {}): Promise<AdjustmentsListResult> {
  const query = toQuery(filters)
  const data = await fetchJson<unknown>(`/adjustments${query}`)
  return AdjustmentsListResultSchema.parse(data)
}

export async function getAdjustment(id: string): Promise<AdjustmentDetail> {
  const data = await fetchJson<unknown>(`/adjustments/${id}`)
  return AdjustmentDetailSchema.parse(data)
}

export async function createAdjustment(dto: CreateAdjustmentDto): Promise<{ id: string }> {
  const payload = CreateAdjustmentSchema.parse(dto)
  return fetchJson<{ id: string }>(`/adjustments`, { method: 'POST', json: payload })
}

export async function addAdjustmentLine(adjustmentId: string, dto: AddAdjustmentLineDto): Promise<{ id: string }> {
  const payload = AddAdjustmentLineSchema.parse(dto)
  return fetchJson<{ id: string }>(`/adjustments/${adjustmentId}/lines`, { method: 'POST', json: payload })
}

export async function removeAdjustmentLine(adjustmentId: string, lineId: string): Promise<void> {
  return fetchJson<void>(`/adjustments/${adjustmentId}/lines/${lineId}`, { method: 'DELETE' })
}

export async function updateAdjustmentHeader(adjustmentId: string, dto: UpdateAdjustmentHeaderDto): Promise<void> {
  const payload = UpdateAdjustmentHeaderSchema.parse(dto)
  return fetchJson<void>(`/adjustments/${adjustmentId}`, { method: 'PUT', json: payload })
}

export async function updateAdjustmentLine(
  adjustmentId: string,
  lineId: string,
  dto: UpdateAdjustmentLineDto
): Promise<void> {
  const payload = UpdateAdjustmentLineSchema.parse(dto)
  return fetchJson<void>(`/adjustments/${adjustmentId}/lines/${lineId}`, { method: 'PUT', json: payload })
}

export async function postAdjustment(adjustmentId: string): Promise<void> {
  return fetchJson<void>(`/adjustments/${adjustmentId}/post`, { method: 'POST' })
}

export async function cancelAdjustment(adjustmentId: string): Promise<void> {
  return fetchJson<void>(`/adjustments/${adjustmentId}/cancel`, { method: 'POST' })
}

