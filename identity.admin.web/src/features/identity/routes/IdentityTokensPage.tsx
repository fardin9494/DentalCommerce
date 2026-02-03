import { FormEvent, useState } from 'react'
import { fetchJson } from '@/lib/api/client'
import { useAdminAuth } from '@/app/auth'
import { useToast } from '@/shared/components/toast/ToastProvider'

type TokensDto = {
  accessToken: string
  accessTokenExpiresIn: number
  refreshToken: string
  userId: string
  siteId: string
}

export function IdentityTokensPage() {
  const toast = useToast()
  const { accessToken, refreshToken, userId, siteId, setAuth, clear } = useAdminAuth()
  const [busy, setBusy] = useState(false)

  async function refresh(e: FormEvent) {
    e.preventDefault()
    if (!refreshToken) {
      toast.error('RefreshToken موجود نیست')
      return
    }
    setBusy(true)
    try {
      const res = await fetchJson<TokensDto>('/auth/refresh', {
        method: 'POST',
        json: { refreshToken },
      })
      setAuth({
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        siteId: res.siteId,
        userId: res.userId,
      })
      toast.success('توکن جدید صادر شد')
    } catch (err: any) {
      toast.error(err?.message || 'رفرش ناموفق بود')
    } finally {
      setBusy(false)
    }
  }

  async function logout() {
    setBusy(true)
    try {
      await fetchJson<void>('/auth/logout', { method: 'POST' })
    } catch {}
    clear()
  }

  return (
    <div className="space-y-6" dir="rtl">
      <h1 className="text-lg font-semibold">توکن‌ها</h1>

      <div className="card space-y-2 text-sm">
        <div><span className="label">UserId:</span> <span className="value">{userId || '-'}</span></div>
        <div><span className="label">SiteId:</span> <span className="value">{siteId || '-'}</span></div>
        <div><span className="label">AccessToken:</span></div>
        <textarea className="input h-28 font-mono text-xs" readOnly value={accessToken ?? ''} />
        <div><span className="label">RefreshToken:</span></div>
        <textarea className="input h-24 font-mono text-xs" readOnly value={refreshToken ?? ''} />
      </div>

      <div className="flex gap-3">
        <form onSubmit={refresh}>
          <button className="btn btn-primary" disabled={busy}>رفرش توکن</button>
        </form>
        <button className="btn btn-ghost" disabled={busy} onClick={logout}>خروج</button>
      </div>
    </div>
  )
}
