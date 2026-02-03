import { FormEvent, useEffect, useState } from 'react'
import { fetchJson } from '@/lib/api/client'
import { useToast } from '@/shared/components/toast/ToastProvider'
import { formatDate, jalaliInputToIso, toJalaliDateString } from '@/shared/utils/date'

type MeDto = {
  userId: string
  phoneNumber: string
  fullName: string | null
  nationalId: string | null
  address: string | null
  postalCode: string | null
  landline: string | null
  birthDateUtc: string | null
  phoneVerifiedAtUtc: string | null
  hasPassword: boolean
  createdAtUtc: string
}

type SiteDto = {
  siteId: string
  joinedAtUtc: string
}

export function IdentityMePage() {
  const toast = useToast()
  const [me, setMe] = useState<MeDto | null>(null)
  const [sites, setSites] = useState<SiteDto[]>([])
  const [fullName, setFullName] = useState('')
  const [nationalId, setNationalId] = useState('')
  const [address, setAddress] = useState('')
  const [postalCode, setPostalCode] = useState('')
  const [landline, setLandline] = useState('')
  const [birthDateShamsi, setBirthDateShamsi] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)

  async function load() {
    setBusy(true)
    try {
      const data = await fetchJson<MeDto>('/me')
      setMe(data)
      setFullName(data.fullName ?? '')
      setNationalId(data.nationalId ?? '')
      setAddress(data.address ?? '')
      setPostalCode(data.postalCode ?? '')
      setLandline(data.landline ?? '')
      setBirthDateShamsi(toJalaliDateString(data.birthDateUtc))
      const memberships = await fetchJson<SiteDto[]>('/me/sites')
      setSites(memberships)
    } catch (err: any) {
      toast.error(err?.message || 'خطا در دریافت اطلاعات')
    } finally {
      setBusy(false)
    }
  }

  useEffect(() => {
    load()
  }, [])

  async function updateMe(e: FormEvent) {
    e.preventDefault()
    const trimmedBirth = birthDateShamsi.trim()
    const birthDateUtc = trimmedBirth ? jalaliInputToIso(trimmedBirth) : null
    if (trimmedBirth && !birthDateUtc) {
      toast.error('تاریخ تولد نامعتبر است. مثال: ۱۳۷۰/۰۵/۲۲')
      return
    }
    setBusy(true)
    try {
      const data = await fetchJson<MeDto>('/me', {
        method: 'PATCH',
        json: {
          fullName: fullName.trim() || null,
          nationalId: nationalId.trim() || null,
          address: address.trim() || null,
          postalCode: postalCode.trim() || null,
          landline: landline.trim() || null,
          birthDateUtc
        }
      })
      setMe(data)
      setBirthDateShamsi(toJalaliDateString(data.birthDateUtc))
      toast.success('اطلاعات ذخیره شد')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در ذخیره')
    } finally {
      setBusy(false)
    }
  }

  async function submitPassword(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    try {
      await fetchJson<void>('/me/password/set', { method: 'POST', json: { password: password.trim() } })
      setPassword('')
      toast.success('رمز عبور ثبت شد')
    } catch (err: any) {
      toast.error(err?.message || 'خطا در تنظیم رمز عبور')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="space-y-6" dir="rtl">
      <div className="flex items-center justify-between">
        <h1 className="text-lg font-semibold">پروفایل کاربر</h1>
        <button className="btn btn-ghost" onClick={load} disabled={busy}>بارگذاری مجدد</button>
      </div>

      <div className="card">
        <div className="grid md:grid-cols-4 gap-3 text-sm">
          <div><div className="label">شناسه</div><div className="value">{me?.userId || '-'}</div></div>
          <div><div className="label">موبایل</div><div className="value">{me?.phoneNumber || '-'}</div></div>
          <div><div className="label">تأیید موبایل</div><div className="value">{me?.phoneVerifiedAtUtc ? 'تأیید شده' : 'نامشخص'}</div></div>
          <div><div className="label">رمز عبور</div><div className="value">{me?.hasPassword ? 'دارد' : 'ندارد'}</div></div>
          <div><div className="label">تاریخ ایجاد حساب</div><div className="value">{me?.createdAtUtc ? formatDate(me.createdAtUtc) : '-'}</div></div>
        </div>
      </div>

      <div className="grid lg:grid-cols-2 gap-6">
        <div className="card">
          <h2 className="text-sm font-semibold mb-3">ویرایش مشخصات</h2>
          <form onSubmit={updateMe} className="space-y-3">
            <input className="input" placeholder="نام کامل" value={fullName} onChange={e => setFullName(e.target.value)} />
            <input className="input" placeholder="کد ملی (اختیاری)" value={nationalId} onChange={e => setNationalId(e.target.value)} />
            <div className="grid md:grid-cols-2 gap-3">
              <input className="input" placeholder="کد پستی (اختیاری)" value={postalCode} onChange={e => setPostalCode(e.target.value)} />
              <input className="input" placeholder="تلفن ثابت (اختیاری)" value={landline} onChange={e => setLandline(e.target.value)} />
            </div>
            <input
              className="input"
              placeholder="تاریخ تولد شمسی (مثال ۱۳۷۰/۰۵/۲۲)"
              value={birthDateShamsi}
              onChange={e => setBirthDateShamsi(e.target.value)}
            />
            <textarea
              className="input"
              rows={3}
              placeholder="آدرس کامل منزل (اختیاری)"
              value={address}
              onChange={e => setAddress(e.target.value)}
            />
            <button className="btn btn-primary" disabled={busy}>ذخیره</button>
          </form>
        </div>

        <div className="card">
          <h2 className="text-sm font-semibold mb-3">تنظیم رمز عبور</h2>
          <form onSubmit={submitPassword} className="space-y-3">
            <input className="input" type="password" placeholder="رمز عبور جدید" value={password} onChange={e => setPassword(e.target.value)} />
            <button className="btn btn-indigo" disabled={busy}>ثبت رمز</button>
          </form>
        </div>
      </div>

      <div className="card">
        <h2 className="text-sm font-semibold mb-3">سایت‌های عضو</h2>
        {sites.length === 0 ? (
          <div className="text-xs text-gray-500">عضویتی ثبت نشده است</div>
        ) : (
          <div className="grid md:grid-cols-2 gap-3 text-sm">
            {sites.map(s => (
              <div key={s.siteId} className="border rounded p-3">
                <div className="label">SiteId</div>
                <div className="value">{s.siteId}</div>
                <div className="label mt-2">تاریخ عضویت</div>
                <div className="value">{formatDate(s.joinedAtUtc)}</div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
