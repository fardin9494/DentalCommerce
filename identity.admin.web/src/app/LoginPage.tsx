import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from 'react'
import { useAdminAuth } from './auth'
import { fetchJson } from '@/lib/api/client'
import { DEFAULT_SITE_ID } from './env'

type TokensDto = {
  accessToken: string
  accessTokenExpiresIn: number
  refreshToken: string
  userId: string
  siteId: string
}

type LoginMode = 'otp' | 'password'

function getFriendlyError(err: any, context: 'otp-request' | 'otp-verify' | 'password-login') {
  const status = err?.status as number | undefined
  const backendMessage = (err?.message as string | undefined)?.trim()

  if (backendMessage && backendMessage.length > 0 && backendMessage.length < 160) {
    return backendMessage
  }

  if (context === 'otp-request') {
    if (status === 429) return 'درخواست‌های زیادی ارسال شده است. لطفاً چند لحظه صبر کنید و دوباره تلاش کنید.'
    if (status === 400) return 'شماره موبایل یا اطلاعات وارد شده معتبر نیست. لطفاً بررسی کنید و دوباره تلاش کنید.'
    return 'در ارسال کد تایید مشکلی پیش آمد. لطفاً بعد از چند لحظه دوباره تلاش کنید.'
  }

  if (context === 'otp-verify') {
    if (status === 400 || status === 401) return 'کد وارد شده معتبر نیست یا منقضی شده است. لطفاً دوباره درخواست کد جدید بدهید.'
    if (status === 423)
      return 'حساب شما به‌صورت موقت قفل شده است. اگر این مشکل ادامه داشت، با پشتیبانی تماس بگیرید.'
    if (status === 403)
      return 'دسترسی شما به این پنل محدود شده است. برای رفع مشکل با پشتیبانی مجموعه هماهنگ کنید.'
    return 'تایید کد با خطا مواجه شد. لطفاً دوباره تلاش کنید.'
  }

  if (context === 'password-login') {
    if (status === 400 || status === 401) return 'شماره موبایل یا رمز عبور صحیح نیست.'
    if (status === 403)
      return 'حساب شما برای ورود به این پنل مجاز نیست. در صورت نیاز با مدیر سیستم یا پشتیبانی تماس بگیرید.'
    if (status === 423)
      return 'حساب شما قفل شده است. لطفاً کمی بعد دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.'
    if (status === 429) return 'تعداد تلاش‌های ناموفق زیاد بوده است. چند دقیقه صبر کنید و سپس دوباره تلاش کنید.'
    return 'در ورود با رمز عبور خطایی رخ داد. لطفاً بعداً دوباره تلاش کنید.'
  }

  return 'خطایی رخ داد. لطفاً دوباره تلاش کنید.'
}

function formatSecondsToClock(totalSeconds: number) {
  const seconds = Math.max(0, Math.floor(totalSeconds))
  const m = Math.floor(seconds / 60)
  const s = seconds % 60
  return `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
}

export function LoginPage() {
  const { setAuth } = useAdminAuth()
  const [phone, setPhone] = useState('')
  const [siteId] = useState(DEFAULT_SITE_ID)
  const [otp, setOtp] = useState('')
  const [password, setPassword] = useState('')
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loginMode, setLoginMode] = useState<LoginMode>('otp')
  const [otpStep, setOtpStep] = useState<'phone' | 'code'>('phone')
  const [otpRemainingSeconds, setOtpRemainingSeconds] = useState<number | null>(null)
  const [isRequestingOtp, setIsRequestingOtp] = useState(false)
  const [isVerifyingOtp, setIsVerifyingOtp] = useState(false)
  const [isPasswordLogin, setIsPasswordLogin] = useState(false)
  const otpInputsRef = useRef<Array<HTMLInputElement | null>>([])

  useEffect(() => {
    if (otpRemainingSeconds == null || otpRemainingSeconds <= 0) return
    const intervalId = window.setInterval(() => {
      setOtpRemainingSeconds((prev) => {
        if (prev == null) return null
        if (prev <= 1) return 0
        return prev - 1
      })
    }, 1000)

    return () => {
      window.clearInterval(intervalId)
    }
  }, [otpRemainingSeconds])

  useEffect(() => {
    if (otpStep === 'code') {
      otpInputsRef.current[0]?.focus()
    }
  }, [otpStep])

  function resetMessages() {
    setError(null)
    setMessage(null)
  }

  async function requestOtp() {
    resetMessages()
    setIsRequestingOtp(true)
    try {
      const res = await fetchJson<{ ttlSeconds: number }>('/auth/otp/request', {
        json: { phoneNumber: phone.trim(), siteId: siteId.trim() },
      })
      const ttlSeconds = Number.isFinite(res.ttlSeconds) && res.ttlSeconds > 0 ? res.ttlSeconds : 180
      setOtp('')
      setOtpStep('code')
      setOtpRemainingSeconds(ttlSeconds)
      setMessage('کد تایید برای شما ارسال شد. لطفاً کد را در مدت زمان مشخص شده وارد کنید.')
    } catch (err: any) {
      setError(getFriendlyError(err, 'otp-request'))
    } finally {
      setIsRequestingOtp(false)
    }
  }

  async function verifyOtp(e: FormEvent) {
    e.preventDefault()
    resetMessages()
    setIsVerifyingOtp(true)
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
      setOtpRemainingSeconds(null)
    } catch (err: any) {
      setError(getFriendlyError(err, 'otp-verify'))
    } finally {
      setIsVerifyingOtp(false)
    }
  }

  async function passwordLogin(e: FormEvent) {
    e.preventDefault()
    resetMessages()
    setIsPasswordLogin(true)
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
    } catch (err: any) {
      setError(getFriendlyError(err, 'password-login'))
    } finally {
      setIsPasswordLogin(false)
    }
  }

  const otpDigitsCount = 6

  function handleOtpDigitChange(index: number, value: string) {
    const digit = value.replace(/\D/g, '').slice(-1)
    const chars = otp.split('')
    chars[index] = digit
    const next = chars.join('').slice(0, otpDigitsCount)
    setOtp(next)

    if (digit && index < otpDigitsCount - 1) {
      otpInputsRef.current[index + 1]?.focus()
    }
  }

  function handleOtpKeyDown(index: number, e: KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      otpInputsRef.current[index - 1]?.focus()
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-slate-50 via-slate-100 to-slate-200 px-4">
      <div className="w-full max-w-xl bg-white/90 backdrop-blur border border-slate-200 rounded-2xl shadow-lg shadow-slate-200/60 p-6 md:p-8">
        <div className="mb-6 text-center">
          <h1 className="text-xl font-semibold text-slate-900">ورود به پنل هویت</h1>
          <p className="mt-1 text-xs text-slate-500">برای مدیریت هویت کاربران وارد حساب خود شوید.</p>
        </div>

        <div className="mb-6 flex items-center justify-center">
          <div className="inline-flex w-full max-w-sm items-center justify-between rounded-full bg-slate-100 p-1 text-xs font-medium">
            <button
              type="button"
              onClick={() => {
                setLoginMode('otp')
                resetMessages()
              }}
              className={`flex-1 rounded-full px-3 py-2 transition ${
                loginMode === 'otp'
                  ? 'bg-white shadow-sm text-slate-900'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              ورود با کد یکبار مصرف
            </button>
            <button
              type="button"
              onClick={() => {
                setLoginMode('password')
                resetMessages()
              }}
              className={`flex-1 rounded-full px-3 py-2 transition ${
                loginMode === 'password'
                  ? 'bg-white shadow-sm text-slate-900'
                  : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              ورود با رمز عبور
            </button>
          </div>
        </div>

        {loginMode === 'otp' ? (
          <form
            onSubmit={otpStep === 'code' ? verifyOtp : (e) => {
              e.preventDefault()
              if (!isRequestingOtp && phone.trim()) {
                requestOtp()
              }
            }}
            className="space-y-4"
          >
            {otpStep === 'phone' ? (
              <div className="space-y-4">
                <div className="space-y-2">
                  <label className="text-xs font-medium text-slate-700">شماره موبایل</label>
                  <input
                    className="w-full rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-emerald-500 focus:bg-white focus:ring-1 focus:ring-emerald-500"
                    placeholder="مثال: 0912xxxxxxx"
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    dir="ltr"
                  />
                </div>
                <button
                  type="submit"
                  disabled={isRequestingOtp || !phone.trim()}
                  className="inline-flex w-full items-center justify-center rounded-lg bg-slate-900 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isRequestingOtp ? 'در حال ارسال کد...' : 'ارسال کد'}
                </button>
              </div>
            ) : (
              <div className="space-y-4">
                <div className="space-y-1">
                  <p className="text-xs text-slate-600">
                    کد یکبار مصرف به شماره وارد شده ارسال شد. لطفاً کد ۶ رقمی را وارد کنید.
                  </p>
                  {otpRemainingSeconds != null && otpRemainingSeconds > 0 && (
                    <div className="inline-flex items-center gap-2 rounded-full bg-emerald-50 px-3 py-1.5 text-[11px] text-emerald-700">
                      <span>زمان باقی‌مانده:</span>
                      <span className="font-mono text-xs font-semibold">
                        {formatSecondsToClock(otpRemainingSeconds)}
                      </span>
                    </div>
                  )}
                  {otpRemainingSeconds === 0 && (
                    <div className="rounded-lg bg-amber-50 px-3 py-2 text-[11px] text-amber-800">
                      زمان استفاده از این کد به پایان رسیده است. برای دریافت کد جدید، دوباره «ارسال کد» را بزنید.
                    </div>
                  )}
                </div>

                <div className="flex justify-between gap-2" dir="ltr">
                  {Array.from({ length: otpDigitsCount }).map((_, index) => (
                    <input
                      key={index}
                      ref={(el) => {
                        otpInputsRef.current[index] = el
                      }}
                      type="text"
                      inputMode="numeric"
                      pattern="[0-9]*"
                      maxLength={1}
                      className="h-11 w-10 rounded-lg border border-slate-200 bg-white text-center text-lg font-medium tracking-widest text-slate-900 outline-none transition focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
                      value={otp[index] ?? ''}
                      onChange={(e) => handleOtpDigitChange(index, e.target.value)}
                      onKeyDown={(e) => handleOtpKeyDown(index, e)}
                    />
                  ))}
                </div>

                <button
                  type="submit"
                  disabled={isVerifyingOtp || otp.length !== otpDigitsCount || otpRemainingSeconds === 0}
                  className="inline-flex w-full items-center justify-center rounded-lg bg-emerald-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isVerifyingOtp ? 'در حال ورود...' : 'تایید کد و ورود'}
                </button>
              </div>
            )}
          </form>
        ) : (
          <form onSubmit={passwordLogin} className="space-y-4">
            <div className="space-y-2">
              <label className="text-xs font-medium text-slate-700">شماره موبایل</label>
              <input
                className="w-full rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-indigo-500 focus:bg-white focus:ring-1 focus:ring-indigo-500"
                placeholder="مثال: 0912xxxxxxx"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                dir="ltr"
              />
            </div>

            <div className="space-y-2">
              <label className="text-xs font-medium text-slate-700">رمز عبور</label>
              <input
                type="password"
                className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500"
                placeholder="رمز عبور خود را وارد کنید"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>

            <button
              type="submit"
              disabled={isPasswordLogin || !phone.trim() || !password.trim()}
              className="mt-1 inline-flex w-full items-center justify-center rounded-lg bg-indigo-600 px-4 py-2.5 text-sm font-medium text-white shadow-sm transition hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isPasswordLogin ? 'در حال ورود...' : 'ورود با رمز عبور'}
            </button>
          </form>
        )}

        <div className="mt-5 space-y-2">
          {message && (
            <div className="flex items-start gap-2 rounded-lg border border-emerald-100 bg-emerald-50 px-3 py-2 text-[11px] text-emerald-800">
              <span className="mt-0.5 h-1.5 w-1.5 rounded-full bg-emerald-500" />
              <span>{message}</span>
            </div>
          )}
          {error && (
            <div className="flex items-start gap-2 rounded-lg border border-red-100 bg-red-50 px-3 py-2 text-[11px] text-red-700">
              <span className="mt-0.5 h-1.5 w-1.5 rounded-full bg-red-500" />
              <span>{error}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
