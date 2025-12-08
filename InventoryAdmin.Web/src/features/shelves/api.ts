import { z } from 'zod'
import { fetchJson, toQuery } from '@/lib/api/client'

export const ShelfSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  warehouseName: z.string().nullable().optional(),
  name: z.string(),
  description: z.string().nullable().optional(),
  isActive: z.boolean(),
  createdAt: z.string(),
  updatedAt: z.string(),
})
export type Shelf = z.infer<typeof ShelfSchema>

export const ShelvesListSchema = z.array(ShelfSchema)

export interface CreateShelfDto {
  warehouseId: string
  name: string
  description?: string
}

export interface UpdateShelfDto {
  name: string
  description?: string
}

export async function getShelves(warehouseId?: string, isActive?: boolean): Promise<Shelf[]> {
  const query = toQuery({ warehouseId, isActive })
  const data = await fetchJson<unknown>(`/shelves${query}`)
  return ShelvesListSchema.parse(data)
}

export async function createShelf(dto: CreateShelfDto): Promise<{ id: string }> {
  return fetchJson<{ id: string }>('/shelves', { method: 'POST', json: dto })
}

export async function updateShelf(id: string, dto: UpdateShelfDto): Promise<void> {
  return fetchJson<void>(`/shelves/${id}`, { method: 'PUT', json: dto })
}

export async function deactivateShelf(id: string): Promise<void> {
  return fetchJson<void>(`/shelves/${id}/deactivate`, { method: 'POST' })
}

