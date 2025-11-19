import { FormEvent, useState } from 'react'
import { useAdminAuth } from './auth'
import { fetchJson } from '@/lib/api/client'

export function LoginPage() {
  const { setToken } = useAdminAuth()
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    const pwd = password.trim()
    if (!pwd) {
      setError('پسورد را وارد کنید')
      return
    }

    setError(null)
    setSubmitting(true)
    try {
      // Call lightweight protected endpoint to validate password
      await fetchJson<void>('/auth/check', { token: pwd })
      setToken(pwd)
    } catch (err: any) {
      if (err && typeof err.status === 'number' && err.status === 401) {
        setError('پسورد اشتباه است')
      } else {
        setError('خطا در ارتباط با سرور')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="w-full max-w-sm bg-white border rounded-lg shadow-sm p-6">
        <h1 className="text-lg font-semibold mb-4 text-center">ورود به پنل ادمین</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm mb-1">پسورد ادمین</label>
            <input
              type="password"
              className="w-full border rounded px-3 py-2 text-sm"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoFocus
            />
          </div>
          {error && <div className="text-xs text-red-600">{error}</div>}
          <button
            type="submit"
            disabled={submitting}
            className="w-full bg-gray-900 text-white rounded py-2 text-sm hover:bg-gray-800 disabled:opacity-60"
          >
            {submitting ? 'در حال بررسی...' : 'ورود'}
          </button>
          <p className="text-[11px] text-gray-500 text-center mt-2">
            پسورد اینجا فقط در مرورگر شما نگه‌داری می‌شود و به‌صورت هدر روی درخواست‌ها فرستاده می‌شود.
          </p>
        </form>
      </div>
    </div>
  )
}

