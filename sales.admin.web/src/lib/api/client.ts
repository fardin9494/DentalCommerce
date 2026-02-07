import { API_BASE } from '@/app/env'

type ApiError = {
  status: number
  message: string
  details?: unknown
}

type Options = RequestInit & {
  token?: string
  json?: unknown
}

export async function fetchJson<T>(path: string, opts: Options = {}): Promise<T> {
  return fetchJsonWithBase(API_BASE, path, opts)
}

export async function fetchJsonWithBase<T>(base: string, path: string, opts: Options = {}): Promise<T> {
  const headers = new Headers(opts.headers)
  if (opts.json !== undefined && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json')
  if (!headers.has('Accept')) headers.set('Accept', 'application/json')

  const method = (opts.method ?? (opts.json !== undefined ? 'POST' : 'GET')).toUpperCase()

  let token = opts.token
  if (!token && typeof window !== 'undefined') {
    try {
      token = localStorage.getItem('admin_token') ?? undefined
    } catch {
      // ignore storage errors
    }
  }
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const canHaveBody = method !== 'GET' && method !== 'HEAD'
  const body = canHaveBody
    ? (opts.json !== undefined ? JSON.stringify(opts.json) : opts.body)
    : undefined

  const res = await fetch(`${base}${path}`.replace(/\/$/, ''), {
    ...opts,
    method,
    headers,
    body,
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
