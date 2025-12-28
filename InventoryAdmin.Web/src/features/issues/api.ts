import { fetchJson } from '@/lib/api/client'
import { IssueDetailSchema, CreateIssueSchema, AddIssueLineSchema, UpdateIssueHeaderSchema, UpdateIssueLineSchema, IssuesListResultSchema, IssueLineSerialOptionSchema, type IssueDetail, type CreateIssueDto, type AddIssueLineDto, type UpdateIssueHeaderDto, type UpdateIssueLineDto, type IssuesListResult, type IssuesListFilters, type IssueLineSerialOption } from './types'

export async function getIssuesList(filters: IssuesListFilters = {}): Promise<IssuesListResult> {
  const params = new URLSearchParams()
  if (filters.page) params.set('page', filters.page.toString())
  if (filters.pageSize) params.set('pageSize', filters.pageSize.toString())
  if (filters.warehouseId) params.set('warehouseId', filters.warehouseId)
  if (filters.status !== undefined) params.set('status', filters.status.toString())
  if (filters.fromDate) params.set('fromDate', filters.fromDate)
  if (filters.toDate) params.set('toDate', filters.toDate)
  if (filters.search) params.set('search', filters.search)

  const queryString = params.toString()
  const url = `/issues${queryString ? `?${queryString}` : ''}`
  const data = await fetchJson<unknown>(url)
  return IssuesListResultSchema.parse(data)
}

export async function getIssue(id: string): Promise<IssueDetail> {
  const data = await fetchJson<unknown>(`/issues/${id}`)
  return IssueDetailSchema.parse(data)
}

export async function createIssue(dto: CreateIssueDto): Promise<{ id: string }> {
  const payload = CreateIssueSchema.parse(dto)
  return fetchJson<{ id: string }>(`/issues`, { method: 'POST', json: payload })
}

export async function addIssueLine(issueId: string, dto: AddIssueLineDto): Promise<{ id: string }> {
  const payload = AddIssueLineSchema.parse(dto)
  return fetchJson<{ id: string }>(`/issues/${issueId}/lines`, { method: 'POST', json: payload })
}

export async function removeIssueLine(issueId: string, lineId: string): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/lines/${lineId}`, { method: 'DELETE' })
}

export async function updateIssueHeader(issueId: string, dto: UpdateIssueHeaderDto): Promise<void> {
  const payload = UpdateIssueHeaderSchema.parse(dto)
  return fetchJson<void>(`/issues/${issueId}`, { method: 'PUT', json: payload })
}

export async function updateIssueLine(issueId: string, lineId: string, dto: UpdateIssueLineDto): Promise<void> {
  const payload = UpdateIssueLineSchema.parse(dto)
  return fetchJson<void>(`/issues/${issueId}/lines/${lineId}`, { method: 'PUT', json: payload })
}

export async function allocateIssueLineFefo(issueId: string, lineId: string, preferredWarehouseId?: string): Promise<unknown> {
  return fetchJson<unknown>(`/issues/${issueId}/lines/${lineId}/allocate-fefo`, { 
    method: 'POST', 
    json: preferredWarehouseId || null 
  })
}

export async function allocateIssueLineFifo(issueId: string, lineId: string, preferredWarehouseId?: string): Promise<unknown> {
  return fetchJson<unknown>(`/issues/${issueId}/lines/${lineId}/allocate-fifo`, { 
    method: 'POST', 
    json: preferredWarehouseId || null 
  })
}

export async function allocateIssueLineLifo(issueId: string, lineId: string, preferredWarehouseId?: string): Promise<unknown> {
  return fetchJson<unknown>(`/issues/${issueId}/lines/${lineId}/allocate-lifo`, { 
    method: 'POST', 
    json: preferredWarehouseId || null 
  })
}

export async function getIssueLineAvailableSerials(issueId: string, lineId: string, warehouseId?: string): Promise<IssueLineSerialOption[]> {
  const params = new URLSearchParams()
  if (warehouseId) params.set('warehouseId', warehouseId)
  const query = params.toString()
  const data = await fetchJson<unknown>(`/issues/${issueId}/lines/${lineId}/available-serials${query ? `?${query}` : ''}`)
  return IssueLineSerialOptionSchema.array().parse(data)
}

export async function allocateIssueLineSerials(issueId: string, lineId: string, serials: string[]): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/lines/${lineId}/allocate-serials`, { method: 'POST', json: { serials } })
}

export async function postIssue(issueId: string, whenUtc?: string): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/post`, { method: 'POST', json: whenUtc || null })
}

export async function cancelIssue(issueId: string): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/cancel`, { method: 'POST' })
}



