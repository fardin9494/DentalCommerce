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
  if (opts.token) headers.set('Authorization', `Bearer ${opts.token}`)

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

export function apiOrigin() {
  try {
    const url = new URL(API_BASE, location.origin)
    return url.origin
  } catch {
    return location.origin
  }
}

export function toPublicMediaUrl(path?: string | null) {
  if (!path) return undefined
  if (/^https?:\/\//i.test(path)) return path
  const cleaned = path.replace(/\\/g, '/').replace(/^\/+/, '')
  const p = cleaned.startsWith('media/') || cleaned.startsWith('/media/') ? `/${cleaned.replace(/^\/+/, '')}` : `/media/${cleaned}`
  return `${apiOrigin()}${p}`
}
