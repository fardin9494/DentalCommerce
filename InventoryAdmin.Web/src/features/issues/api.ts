import { fetchJson } from '@/lib/api/client'
import { IssueDetailSchema, CreateIssueSchema, AddIssueLineSchema, UpdateIssueHeaderSchema, UpdateIssueLineSchema, type IssueDetail, type CreateIssueDto, type AddIssueLineDto, type UpdateIssueHeaderDto, type UpdateIssueLineDto } from './types'

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

export async function allocateIssueLineFefo(issueId: string, lineId: string): Promise<unknown> {
  return fetchJson<unknown>(`/issues/${issueId}/lines/${lineId}/allocate-fefo`, { method: 'POST' })
}

export async function postIssue(issueId: string, whenUtc?: string): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/post`, { method: 'POST', json: whenUtc || null })
}

export async function cancelIssue(issueId: string): Promise<void> {
  return fetchJson<void>(`/issues/${issueId}/cancel`, { method: 'POST' })
}


