import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { fetchJson } from '@/lib/api/client'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { PermissionToggleList } from '../components/PermissionToggleList'

type SiteDto = { siteId: string; joinedAtUtc: string }
type SessionDto = {
  sessionId: string
  siteId: string
  createdAtUtc: string
  expiresAtUtc: string
  revokedAtUtc: string | null
  userAgent: string | null
  ipAddress: string | null
}

type AuditItem = {
  id: string
  eventType: string
  title: string
  detail: string | null
  actorType: string
  actorUserId: string | null
  ipAddress: string | null
  userAgent: string | null
  occurredAtUtc: string
}

type AuditList = {
  total: number
  items: AuditItem[]
}

type PermissionItem = {
  key: string
  title: string
  description: string
  granted: boolean
}

type InventoryPermissionDto = {
  userId: string
  isSuperAdmin: boolean
  uiPolicy: 'Hide' | 'Disable'
  items: PermissionItem[]
}

type UserSummary = {
  userId: string
  phoneNumber: string
  fullName: string | null
  createdAtUtc: string
  lastLoginAtUtc: string | null
  isBanned: boolean
  banUntilUtc: string | null
  banReason: string | null
  lockedUntilUtc: string | null
  failedCount: number
  hasPassword: boolean
}

type DetailsDto = {
  user: UserSummary
  sites: SiteDto[]
  sessions: SessionDto[]
}

export function IdentityUserDetailsPage() {
  const toast = useToast()
  const { id } = useParams()
  const navigate = useNavigate()
  const [data, setData] = useState<DetailsDto | null>(null)
  const [audit, setAudit] = useState<AuditList | null>(null)
  const [inventoryPermissions, setInventoryPermissions] = useState<InventoryPermissionDto | null>(null)
  const [busy, setBusy] = useState(false)
  const [banReason, setBanReason] = useState('')
  const [banUntil, setBanUntil] = useState('')
  const [newPassword, setNewPassword] = useState('')

  async function load() {
    if (!id) return
    setBusy(true)
    try {
      const res = await fetchJson<DetailsDto>(`/admin/users/${id}`)
      const auditRes = await fetchJson<AuditList>(`/admin/users/${id}/audit`)
      const permRes = await fetchJson<InventoryPermissionDto>(`/admin/users/${id}/permissions/inventory`)
      setData(res)
      setAudit(auditRes)
      setInventoryPermissions(permRes)
      setBanReason(res.user.banReason || '')
      setBanUntil(res.user.banUntilUtc ? toLocalInput(res.user.banUntilUtc) : '')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در دریافت اطلاعات')
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => { load() }, [id])

  async function ban() {
    if (!id) return
    setBusy(true)
    try {
      const untilUtc = banUntil ? new Date(banUntil).toISOString() : null
      await fetchJson<void>(`/admin/users/${id}/ban`, {
        method: 'POST',
        json: { reason: banReason || null, untilUtc },
      })
      toast.success('کاربر بن شد')
      await load()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در بن کردن')
    } finally {
      setBusy(false)
    }
  }

  async function unban() {
    if (!id) return
    setBusy(true)
    try {
      await fetchJson<void>(`/admin/users/${id}/unban`, { method: 'POST' })
      toast.success('بن برداشته شد')
      await load()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در آن‌بن کردن')
    } finally {
      setBusy(false)
    }
  }

  async function unlock() {
    if (!id) return
    setBusy(true)
    try {
      await fetchJson<void>(`/admin/users/${id}/unlock`, { method: 'POST' })
      toast.success('قفل تلاش‌ها پاک شد')
      await load()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در آنلاک')
    } finally {
      setBusy(false)
    }
  }

  async function revokeSessions() {
    if (!id) return
    setBusy(true)
    try {
      await fetchJson<void>(`/admin/users/${id}/sessions/revoke`, { method: 'POST' })
      toast.success('نشست‌ها باطل شد')
      await load()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در ابطال نشست‌ها')
    } finally {
      setBusy(false)
    }
  }

  async function resetPassword(clear: boolean) {
    if (!id) return
    setBusy(true)
    try {
      await fetchJson<void>(`/admin/users/${id}/password/reset`, {
        method: 'POST',
        json: { newPassword: clear ? null : newPassword.trim(), clear },
      })
      setNewPassword('')
      toast.success(clear ? 'رمز حذف شد' : 'رمز جدید ثبت شد')
      await load()
    } catch (err: any) {
      toast.error(err?.message || 'خطا در تغییر رمز')
    } finally {
      setBusy(false)
    }
  }

  function updatePermission(key: string, granted: boolean) {
    if (!inventoryPermissions) return
    const items = inventoryPermissions.items.map(p => (p.key === key ? { ...p, granted } : p))
    setInventoryPermissions({ ...inventoryPermissions, items })
  }

  async function saveInventoryPermissions() {
    if (!id || !inventoryPermissions) return
    setBusy(true)
    try {
      await fetchJson<void>(`/admin/users/${id}/permissions/inventory`, {
        method: 'POST',
        json: {
          uiPolicy: inventoryPermissions.uiPolicy,
          permissions: inventoryPermissions.items.map(p => ({ key: p.key, granted: p.granted })),
        },
      })
      toast.success('دسترسی‌ها ذخیره شد')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در ذخیره دسترسی‌ها')
    } finally {
      setBusy(false)
    }
  }

  const u = data?.user

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold">جزئیات کاربر</h1>
        <button className="btn-ghost" onClick={() => navigate(-1)}>بازگشت</button>
      </div>

      <div className="card grid md:grid-cols-3 gap-3 text-sm">
        <div><div className="label">شناسه</div><div className="value">{u?.userId || '-'}</div></div>
        <div><div className="label">موبایل</div><div className="value">{u?.phoneNumber || '-'}</div></div>
        <div><div className="label">نام</div><div className="value">{u?.fullName || '-'}</div></div>
        <div><div className="label">تاریخ ایجاد</div><div className="value">{formatDate(u?.createdAtUtc)}</div></div>
        <div><div className="label">آخرین ورود</div><div className="value">{formatDate(u?.lastLoginAtUtc)}</div></div>
        <div><div className="label">رمز عبور</div><div className="value">{u?.hasPassword ? 'دارد' : 'ندارد'}</div></div>
        <div><div className="label">وضعیت بن</div><div className="value">{u?.isBanned ? 'بن شده' : 'فعال'}</div></div>
        <div><div className="label">علت بن</div><div className="value">{u?.banReason || '-'}</div></div>
        <div><div className="label">بن تا</div><div className="value">{formatDate(u?.banUntilUtc)}</div></div>
        <div><div className="label">لاک تلاش‌ها</div><div className="value">{formatDate(u?.lockedUntilUtc)}</div></div>
        <div><div className="label">تعداد تلاش ناموفق</div><div className="value">{u?.failedCount ?? 0}</div></div>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        <div className="card space-y-3">
          <h2 className="text-sm font-semibold">کنترل بن / آن‌بن</h2>
          <input className="input" placeholder="علت بن (اختیاری)" value={banReason} onChange={e => setBanReason(e.target.value)} />
          <input className="input" type="datetime-local" value={banUntil} onChange={e => setBanUntil(e.target.value)} />
          <div className="flex gap-2">
            <button className="btn-primary" disabled={busy} onClick={ban}>بن کردن</button>
            <button className="btn-ghost" disabled={busy} onClick={unban}>آن‌بن</button>
          </div>
        </div>

        <div className="card space-y-3">
          <h2 className="text-sm font-semibold">کنترل ورود</h2>
          <div className="flex gap-2">
            <button className="btn-ghost" disabled={busy} onClick={unlock}>پاک‌سازی لاک</button>
            <button className="btn-ghost" disabled={busy} onClick={revokeSessions}>ابطال همه نشست‌ها</button>
          </div>
          <div className="mt-2">
            <input className="input" placeholder="رمز جدید (اختیاری)" value={newPassword} onChange={e => setNewPassword(e.target.value)} />
            <div className="flex gap-2 mt-2">
              <button className="btn-indigo" disabled={busy} onClick={() => resetPassword(false)}>ثبت رمز جدید</button>
              <button className="btn-ghost" disabled={busy} onClick={() => resetPassword(true)}>حذف رمز</button>
            </div>
          </div>
        </div>
      </div>

      <div className="card">
        <h2 className="text-sm font-semibold mb-3">سایت‌های عضو</h2>
        {data?.sites?.length ? (
          <div className="grid md:grid-cols-2 gap-3 text-sm">
            {data.sites.map(s => (
              <div key={s.siteId} className="border rounded p-3">
                <div className="label">SiteId</div>
                <div className="value">{s.siteId}</div>
                <div className="label mt-2">تاریخ عضویت</div>
                <div className="value">{formatDate(s.joinedAtUtc)}</div>
              </div>
            ))}
          </div>
        ) : (
          <div className="text-xs text-gray-500">عضویتی ثبت نشده است</div>
        )}
      </div>

      <div className="card">
        <h2 className="text-sm font-semibold mb-3">نشست‌ها</h2>
        {data?.sessions?.length ? (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">SessionId</th>
                  <th className="p-2">Site</th>
                  <th className="p-2">Created</th>
                  <th className="p-2">Expires</th>
                  <th className="p-2">Revoked</th>
                  <th className="p-2">IP</th>
                </tr>
              </thead>
              <tbody>
                {data.sessions.map(s => (
                  <tr key={s.sessionId} className="border-b last:border-0">
                    <td className="p-2 text-xs">{s.sessionId}</td>
                    <td className="p-2 text-xs">{s.siteId}</td>
                    <td className="p-2 text-xs">{formatDate(s.createdAtUtc)}</td>
                    <td className="p-2 text-xs">{formatDate(s.expiresAtUtc)}</td>
                    <td className="p-2 text-xs">{formatDate(s.revokedAtUtc)}</td>
                    <td className="p-2 text-xs">{s.ipAddress || '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-xs text-gray-500">نشستی ثبت نشده است</div>
        )}
      </div>

      <div className="card">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold">دسترسی‌های اینونتوری</h2>
          <button className="btn-ghost" onClick={saveInventoryPermissions} disabled={busy || inventoryPermissions?.isSuperAdmin}>ذخیره</button>
        </div>
        {inventoryPermissions ? (
          <div className="space-y-3 text-sm">
            {inventoryPermissions.isSuperAdmin && (
              <div className="text-xs text-green-700 bg-green-50 border border-green-200 rounded p-2">
                این کاربر ادمین مادر است و همه دسترسی‌ها را دارد.
              </div>
            )}
            <div className="flex items-center gap-4">
              <span className="text-xs text-gray-500">سیاست UI:</span>
              <label className="flex items-center gap-2">
                <input
                  type="radio"
                  name="uiPolicy"
                  checked={inventoryPermissions.uiPolicy === 'Hide'}
                  onChange={() => setInventoryPermissions({ ...inventoryPermissions, uiPolicy: 'Hide' })}
                  disabled={inventoryPermissions.isSuperAdmin}
                />
                مخفی‌سازی دکمه‌ها
              </label>
              <label className="flex items-center gap-2">
                <input
                  type="radio"
                  name="uiPolicy"
                  checked={inventoryPermissions.uiPolicy === 'Disable'}
                  onChange={() => setInventoryPermissions({ ...inventoryPermissions, uiPolicy: 'Disable' })}
                  disabled={inventoryPermissions.isSuperAdmin}
                />
                قفل‌کردن دکمه‌ها
              </label>
            </div>
            <PermissionToggleList
              items={inventoryPermissions.items}
              disabled={inventoryPermissions.isSuperAdmin}
              onChange={updatePermission}
            />
          </div>
        ) : (
          <div className="text-xs text-gray-500">در حال دریافت دسترسی‌ها…</div>
        )}
      </div>

      <div className="card">
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold">رویدادهای حساس</h2>
          <button className="btn-ghost" onClick={load} disabled={busy}>بارگذاری مجدد</button>
        </div>
        {audit?.items?.length ? (
          <div className="space-y-2 text-sm">
            {audit.items.map(item => (
              <details key={item.id} className="border rounded p-3">
                <summary className="flex items-center justify-between cursor-pointer">
                  <span>{item.title}</span>
                  <span className="text-xs text-gray-500">{formatDate(item.occurredAtUtc)}</span>
                </summary>
                <div className="text-xs text-gray-600 mt-2 space-y-1">
                  {item.detail && <div>توضیح: {item.detail}</div>}
                  <div>عامل: {actorLabel(item.actorType)}</div>
                  {item.ipAddress && <div>IP: {item.ipAddress}</div>}
                  {item.userAgent && <div>مرورگر: {item.userAgent}</div>}
                </div>
              </details>
            ))}
          </div>
        ) : (
          <div className="text-xs text-gray-500">رویدادی ثبت نشده است</div>
        )}
      </div>
    </div>
  )
}

function formatDate(value?: string | null) {
  if (!value) return '—'
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleString('fa-IR')
}

function toLocalInput(utc: string) {
  const d = new Date(utc)
  if (Number.isNaN(d.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function actorLabel(actorType: string) {
  if (actorType === 'admin') return 'ادمین'
  if (actorType === 'user') return 'کاربر'
  return 'سیستم'
}
