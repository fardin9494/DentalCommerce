import { FormEvent, useState } from 'react'
import { fetchJson } from '@/lib/api/client'
import { useAdminAuth } from '@/app/auth'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { DEFAULT_SITE_ID } from '@/app/env'

type TokensDto = {
  accessToken: string
  accessTokenExpiresIn: number
  refreshToken: string
  userId: string
  siteId: string
}

export function IdentityAuthPage() {
  const toast = useToast()
  const { setAuth } = useAdminAuth()

  const [phone, setPhone] = useState('')
  const [siteId] = useState(DEFAULT_SITE_ID)
  const [otp, setOtp] = useState('')
  const [password, setPassword] = useState('')
  const [status, setStatus] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function requestOtp(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      const res = await fetchJson<{ ttlSeconds: number }>('/auth/otp/request', {
        json: { phoneNumber: phone.trim(), siteId: siteId.trim() },
      })
      setStatus(`کد تایید ارسال شد. مهلت ${res.ttlSeconds} ثانیه`)
      toast.success('کد ارسال شد')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در ارسال کد')
    } finally {
      setBusy(false)
    }
  }

  async function verifyOtp(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      const res = await fetchJson<TokensDto>('/auth/otp/verify', {
        json: { phoneNumber: phone.trim(), siteId: siteId.trim(), code: otp.trim() },
      })
      setAuth({
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        siteId: res.siteId,
        userId: res.userId,
      })
      setStatus('ورود با OTP انجام شد')
      toast.success('ورود موفق')
    } catch (err: any) {
      toast.error(err?.message || 'کد نامعتبر است')
    } finally {
      setBusy(false)
    }
  }

  async function loginWithPassword(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      const res = await fetchJson<TokensDto>('/auth/password/login', {
        json: {
          phoneNumber: phone.trim(),
          siteId: siteId.trim(),
          password: password.trim(),
        },
      })
      setAuth({
        accessToken: res.accessToken,
        refreshToken: res.refreshToken,
        siteId: res.siteId,
        userId: res.userId,
      })
      setStatus('ورود با پسورد انجام شد')
      toast.success('ورود موفق')
    } catch (err: any) {
      toast.error(err?.message || 'ورود ناموفق بود')
    } finally {
      setBusy(false)
    }
  }

  async function checkAuth() {
    setBusy(true)
    try {
      await fetchJson<void>('/auth/check')
      toast.success('سرویس در دسترس است')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در ارتباط با سرویس')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold">احراز هویت</h1>
        <button className="btn btn-ghost" onClick={checkAuth} disabled={busy}>بررسی سلامت سرویس</button>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        <div className="card">
          <h2 className="text-sm font-semibold mb-3">OTP - ارسال کد</h2>
          <form onSubmit={requestOtp} className="space-y-3">
            <input className="input" placeholder="شماره موبایل" value={phone} onChange={e => setPhone(e.target.value)} />
            <div className="text-xs text-gray-500">SiteId پیش‌فرض: {siteId}</div>
            <button className="btn btn-primary" disabled={busy}>ارسال کد</button>
          </form>
        </div>

        <div className="card">
          <h2 className="text-sm font-semibold mb-3">OTP - تایید کد</h2>
          <form onSubmit={verifyOtp} className="space-y-3">
            <input className="input" placeholder="کد تایید" value={otp} onChange={e => setOtp(e.target.value)} />
            <button className="btn btn-success" disabled={busy}>تایید و ورود</button>
          </form>
        </div>
      </div>

      <div className="card">
        <h2 className="text-sm font-semibold mb-3">ورود با پسورد</h2>
        <form onSubmit={loginWithPassword} className="grid md:grid-cols-3 gap-3">
          <input className="input" placeholder="شماره موبایل" value={phone} onChange={e => setPhone(e.target.value)} />
            <div className="text-xs text-gray-500">SiteId پیش‌فرض: {siteId}</div>
          <input className="input" placeholder="رمز عبور" type="password" value={password} onChange={e => setPassword(e.target.value)} />
          <div className="md:col-span-3">
            <button className="btn btn-indigo" disabled={busy}>ورود</button>
          </div>
        </form>
      </div>

      {status && <div className="text-xs text-emerald-700">{status}</div>}
    </div>
  )
}
