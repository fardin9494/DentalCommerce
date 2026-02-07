import { API_BASE } from '@/app/env'
import type { ApiError } from './types'

type Options = RequestInit & {
  token?: string
  json?: unknown
}

export async function fetchJson<T>(path: string, opts: Options = {}): Promise<T> {
  return fetchJsonWithBase(API_BASE, path, opts)
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

export async function fetchJsonWithBase<T>(base: string, path: string, opts: Options = {}): Promise<T> {
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

  const res = await fetch(`${base}${path}`.replace(/\/$/, ''), {
    ...opts,
    method,
    headers,
    body: shouldSendBody ? (opts.json !== undefined ? JSON.stringify(opts.json) : opts.body) : undefined,
  })

  const isJson = res.headers.get('content-type')?.includes('application/json')
  if (!res.ok) {
    if (res.status === 401) {
      const err: ApiError = { status: res.status, message: 'نیاز به ورود مجدد دارید.', details: null }
      throw err
    }
    if (res.status === 403) {
      const err: ApiError = { status: res.status, message: 'دسترسی ندارید.', details: null }
      throw err
    }

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
