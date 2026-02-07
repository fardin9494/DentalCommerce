import { IDENTITY_API_BASE } from '@/app/env'
import type { ApiError } from './types'

type Options = RequestInit & {
  token?: string
  json?: unknown
}

export async function fetchIdentityJson<T>(path: string, opts: Options = {}): Promise<T> {
  const headers = new Headers(opts.headers)
  if (opts.json !== undefined && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (!headers.has('Accept')) headers.set('Accept', 'application/json')

  let token = opts.token
  if (!token && typeof window !== 'undefined') {
    try {
      token = localStorage.getItem('admin_token') ?? undefined
    } catch {
      // ignore storage errors
    }
  }
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const method = opts.method ?? (opts.json !== undefined ? 'POST' : undefined)
  const shouldSendBody = method !== 'GET' && method !== 'HEAD'

  const res = await fetch(`${IDENTITY_API_BASE}${path}`.replace(/\/$/, ''), {
    ...opts,
    method,
    headers,
    body: shouldSendBody ? (opts.json !== undefined ? JSON.stringify(opts.json) : opts.body) : undefined,
  })

  const isJson = res.headers.get('content-type')?.includes('application/json')
  if (!res.ok) {
    let message = res.statusText
    let details: unknown
    try {
      if (isJson) {
        const data = await res.json()
        message = data.detail || data.message || data.error || data.title || message
        details = data
      } else {
        message = await res.text()
      }
    } catch {}

    const errorMessage = details && typeof details === 'object' && 'error' in details
      ? String((details as { error: unknown }).error)
      : message

    const err: ApiError = { status: res.status, message: errorMessage, details }
    throw err
  }

  if (res.status === 204) return undefined as unknown as T
  return (isJson ? res.json() : (res.text() as unknown)) as T
}
