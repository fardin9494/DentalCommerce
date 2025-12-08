import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'

export const WarehouseSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  isActive: z.boolean(),
})
export type Warehouse = z.infer<typeof WarehouseSchema>

export const WarehousesListSchema = z.array(WarehouseSchema)

export interface CreateWarehouseDto {
  code: string
  name: string
}

export interface UpdateWarehouseDto {
  name: string
}

export async function getWarehouses(isActive?: boolean): Promise<Warehouse[]> {
  const query = toQuery({ isActive })
  const data = await fetchJson<unknown>(`/warehouses${query}`)
  return WarehousesListSchema.parse(data)
}

export async function createWarehouse(dto: CreateWarehouseDto): Promise<{ id: string }> {
  return fetchJson<{ id: string }>('/warehouses', { method: 'POST', json: dto })
}

export async function updateWarehouse(id: string, dto: UpdateWarehouseDto): Promise<void> {
  return fetchJson<void>(`/warehouses/${id}`, { method: 'PUT', json: dto })
}

export async function activateWarehouse(id: string): Promise<void> {
  return fetchJson<void>(`/warehouses/${id}/activate`, { method: 'POST' })
}

export async function deactivateWarehouse(id: string): Promise<void> {
  return fetchJson<void>(`/warehouses/${id}/deactivate`, { method: 'POST' })
}

