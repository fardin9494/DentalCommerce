import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from 'react'
import { useAdminAuth } from './auth'
import { fetchIdentityJson } from '@/lib/api/identityClient'
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
    if (status === 429) return 'درخواست‌های زیادی ارسال شده است. لطفاً کمی صبر کنید و دوباره تلاش کنید.'
    if (status === 400) return 'شماره موبایل یا اطلاعات وارد شده معتبر نیست. لطفاً بررسی کنید.'
    return 'در ارسال کد تایید مشکلی پیش آمد. لطفاً بعداً دوباره تلاش کنید.'
  }

  if (context === 'otp-verify') {
    if (status === 400 || status === 401) return 'کد وارد شده معتبر نیست یا منقضی شده است.'
    if (status === 423) return 'حساب شما به‌صورت موقت قفل شده است. لطفاً بعداً تلاش کنید.'
    if (status === 403) return 'دسترسی شما به این پنل محدود است. با پشتیبانی تماس بگیرید.'
    return 'تایید کد با خطا مواجه شد. لطفاً دوباره تلاش کنید.'
  }

  if (context === 'password-login') {
    if (status === 400 || status === 401) return 'شماره موبایل یا رمز عبور صحیح نیست.'
    if (status === 403) return 'حساب شما برای ورود به این پنل مجاز نیست.'
    if (status === 423) return 'حساب شما قفل شده است. لطفاً بعداً تلاش کنید.'
    if (status === 429) return 'تعداد تلاش‌های ناموفق زیاد بوده است. کمی صبر کنید.'
    return 'در ورود با رمز عبور خطایی رخ داد. لطفاً دوباره تلاش کنید.'
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
  const { setToken } = useAdminAuth()
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
      const res = await fetchIdentityJson<{ ttlSeconds: number }>('/auth/otp/request', {
        json: { phoneNumber: phone.trim(), siteId: siteId.trim() },
      })
      const ttlSeconds = Number.isFinite(res.ttlSeconds) && res.ttlSeconds > 0 ? res.ttlSeconds : 180
      setOtp('')
      setOtpStep('code')
      setOtpRemainingSeconds(ttlSeconds)
      setMessage('کد تایید برای شما ارسال شد. لطفاً کد را وارد کنید.')
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
      const res = await fetchIdentityJson<TokensDto>('/auth/otp/verify', {
        json: { phoneNumber: phone.trim(), siteId: siteId.trim(), code: otp.trim() },
      })
      setToken(res.accessToken)
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
      const res = await fetchIdentityJson<TokensDto>('/auth/password/login', {
        json: {
          phoneNumber: phone.trim(),
          siteId: siteId.trim(),
          password: password.trim(),
        },
      })
      setToken(res.accessToken)
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
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-xl bg-white border rounded-2xl shadow-sm p-6 md:p-8">
        <div className="mb-6 text-center">
          <h1 className="text-xl font-semibold text-slate-900">ورود به پنل ادمین انبار</h1>
          <p className="mt-1 text-xs text-slate-500">برای مدیریت انبار وارد حساب خود شوید.</p>
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

        {message && <div className="mb-4 text-xs text-emerald-700 bg-emerald-50 border border-emerald-200 rounded p-2">{message}</div>}
        {error && <div className="mb-4 text-xs text-red-700 bg-red-50 border border-red-200 rounded p-2">{error}</div>}

        <div className="space-y-4">
          <div>
            <label className="block text-sm mb-1">شماره موبایل</label>
            <input
              className="w-full border rounded px-3 py-2 text-sm"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="مثلا 0912..."
            />
          </div>

          {loginMode === 'otp' ? (
            <form onSubmit={otpStep === 'code' ? verifyOtp : (e) => {
              e.preventDefault()
              if (!isRequestingOtp && phone.trim()) {
                requestOtp()
              }
            }} className="space-y-4">
              {otpStep === 'phone' ? (
                <button
                  type="submit"
                  disabled={isRequestingOtp || !phone.trim()}
                  className="w-full bg-slate-900 text-white rounded py-2 text-sm hover:bg-slate-800 disabled:opacity-60"
                >
                  {isRequestingOtp ? 'در حال ارسال کد...' : 'ارسال کد'}
                </button>
              ) : (
                <div className="space-y-3">
                  <div className="flex items-center justify-center gap-2" dir="ltr">
                    {Array.from({ length: otpDigitsCount }).map((_, index) => (
                      <input
                        key={index}
                        ref={(el) => (otpInputsRef.current[index] = el)}
                        className="w-10 h-10 text-center border rounded text-sm"
                        value={otp[index] ?? ''}
                        onChange={(e) => handleOtpDigitChange(index, e.target.value)}
                        onKeyDown={(e) => handleOtpKeyDown(index, e)}
                        inputMode="numeric"
                      />
                    ))}
                  </div>
                  {otpRemainingSeconds != null && otpRemainingSeconds > 0 && (
                    <div className="text-xs text-slate-500 text-center">زمان باقی‌مانده: {formatSecondsToClock(otpRemainingSeconds)}</div>
                  )}
                  <button
                    type="submit"
                    disabled={isVerifyingOtp || otp.length !== otpDigitsCount || otpRemainingSeconds === 0}
                    className="w-full bg-slate-900 text-white rounded py-2 text-sm hover:bg-slate-800 disabled:opacity-60"
                  >
                    {isVerifyingOtp ? 'در حال ورود...' : 'تایید کد و ورود'}
                  </button>
                </div>
              )}
            </form>
          ) : (
            <form onSubmit={passwordLogin} className="space-y-4">
              <div>
                <label className="block text-sm mb-1">رمز عبور</label>
                <input
                  type="password"
                  className="w-full border rounded px-3 py-2 text-sm"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </div>
              <button
                type="submit"
                disabled={isPasswordLogin || !phone.trim() || !password.trim()}
                className="w-full bg-slate-900 text-white rounded py-2 text-sm hover:bg-slate-800 disabled:opacity-60"
              >
                {isPasswordLogin ? 'در حال ورود...' : 'ورود با رمز عبور'}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
