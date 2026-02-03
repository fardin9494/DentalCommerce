import { useEffect, useState } from 'react'
import { fetchJson } from '@/lib/api/client'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { formatDate, formatNumber } from '@/shared/utils/date'

type DailySignup = { dateUtc: string; count: number }
type ReportDto = {
  totalUsers: number
  activeUsers: number
  bannedUsers: number
  lockedUsers: number
  dailySignups: DailySignup[]
}

export function IdentityReportsPage() {
  const toast = useToast()
  const [days, setDays] = useState(30)
  const [report, setReport] = useState<ReportDto | null>(null)
  const [busy, setBusy] = useState(false)

  async function load() {
    setBusy(true)
    try {
      const res = await fetchJson<ReportDto>(`/admin/reports/summary?days=${days}`)
      setReport(res)
    } catch (err: any) {
      toast.error(err?.message || 'خطا در دریافت گزارش‌ها')
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => { load() }, [days])

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold">گزارش‌های هویت</h1>
        <div className="flex items-center gap-2">
          <select className="input" value={days} onChange={e => setDays(Number(e.target.value))}>
            <option value={7}>۷ روز اخیر</option>
            <option value={30}>۳۰ روز اخیر</option>
            <option value={60}>۶۰ روز اخیر</option>
            <option value={90}>۹۰ روز اخیر</option>
          </select>
          <button className="btn-ghost" onClick={load} disabled={busy}>بارگذاری مجدد</button>
        </div>
      </div>

      <div className="grid md:grid-cols-4 gap-4">
        <div className="card text-sm">
          <div className="label">کل کاربران</div>
          <div className="value text-lg">{formatNumber(report?.totalUsers ?? 0)}</div>
        </div>
        <div className="card text-sm">
          <div className="label">کاربران فعال</div>
          <div className="value text-lg">{formatNumber(report?.activeUsers ?? 0)}</div>
        </div>
        <div className="card text-sm">
          <div className="label">کاربران بن‌شده</div>
          <div className="value text-lg">{formatNumber(report?.bannedUsers ?? 0)}</div>
        </div>
        <div className="card text-sm">
          <div className="label">کاربران قفل‌شده</div>
          <div className="value text-lg">{formatNumber(report?.lockedUsers ?? 0)}</div>
        </div>
      </div>

      <div className="card">
        <h2 className="text-sm font-semibold mb-3">رشد ثبت‌نام</h2>
        {report?.dailySignups?.length ? (
          <div className="overflow-x-auto">
            <table className="min-w-full text-sm text-center">
              <thead>
                <tr className="bg-gray-100">
                  <th className="p-2">تاریخ</th>
                  <th className="p-2">تعداد ثبت‌نام</th>
                </tr>
              </thead>
              <tbody>
                {report.dailySignups.map(d => (
                  <tr key={d.dateUtc} className="border-b last:border-0">
                    <td className="p-2 text-xs">{formatDate(d.dateUtc)}</td>
                    <td className="p-2">{formatNumber(d.count)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="text-xs text-gray-500">داده‌ای موجود نیست</div>
        )}
      </div>
    </div>
  )
}
