import { API_BASE } from '@/app/env'
import type { ApiError } from './types'

type Options = RequestInit & {
  token?: string
  json?: unknown
}

export async function fetchJson<T>(path: string, opts: Options = {}): Promise<T> {
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

  const res = await fetch(`${API_BASE}${path}`.replace(/\/$/, ''), {
    ...opts,
    headers,
    body: opts.json !== undefined ? JSON.stringify(opts.json) : opts.body,
  })

  const isJson = res.headers.get('content-type')?.includes('application/json')
  if (!res.ok) {
    let message = res.statusText
    let details: unknown
    try {
      if (isJson) {
        const data = await res.json()
        message = data.message || data.error || message
        details = data
      } else {
        message = await res.text()
      }
    } catch {}
    const err: ApiError = { status: res.status, message, details }
    throw err
  }

  if (res.status === 204) return undefined as unknown as T
  return (isJson ? res.json() : (res.text() as unknown)) as T
}

export function toQuery(params: Record<string, unknown | undefined>) {
  const sp = new URLSearchParams()
  Object.entries(params).forEach(([k, v]) => {
    if (v === undefined || v === null || v === '') return
    sp.set(k, String(v))
  })
  const q = sp.toString()
  return q ? `?${q}` : ''
}


