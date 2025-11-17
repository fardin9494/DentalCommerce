import { useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useCountries, useCreateCountry, useDeleteCountry, useUpdateCountry } from '../../products/queries'
import { swalPrompt, swalToastSuccess, swalToastError } from '../../../shared/utils/swal'

export function CountriesPage() {
  const { data: countries, isLoading, isError, error } = useCountries()
  const create = useCreateCountry()
  const update = useUpdateCountry()
  const del = useDeleteCountry()
  const [form, setForm] = useState({ code2: 'IR', code3: 'IRN', nameFa: '', nameEn: '', region: '', flagEmoji: '' })

  return (
    <div className="space-y-4">
      <PageHeader title="Countries">مدیریت کشورها</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : isError ? (
            <div className="text-sm text-red-600">
              Failed to load countries{(error as any)?.message ? `: ${(error as any).message}` : ''}.
              Please ensure the backend exposes GET /countries.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gray-100 text-left">
                    <th className="p-2">نام (FA)</th>
                    <th className="p-2">نام (EN)</th>
                    <th className="p-2">کد 2</th>
                    <th className="p-2">کد 3</th>
                    <th className="p-2">قاره</th>
                    <th className="p-2">پرچم</th>
                    <th className="p-2"></th>
                  </tr>
                </thead>
                <tbody>
                  {countries?.map(c => (
                    <tr key={c.code2} className="border-b last:border-0">
                      <td className="p-2">{c.nameFa}</td>
                      <td className="p-2">{c.nameEn}</td>
                      <td className="p-2">{c.code2}</td>
                      <td className="p-2">{c.code3}</td>
                      <td className="p-2">{c.region || '-'}</td>
                      <td className="p-2">{c.flagEmoji || '-'}</td>
                      <td className="p-2 flex items-center gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          const nameFa = await swalPrompt({ title: 'نام فارسی', defaultValue: c.nameFa, required: true, confirmText: 'ذخیره', cancelText: 'لغو' })
                          if (!nameFa) return
                          const nameEn = await swalPrompt({ title: 'نام (EN)', defaultValue: c.nameEn, required: true, confirmText: 'بعدی', cancelText: 'لغو' })
                          if (!nameEn) return
                          const region = await swalPrompt({ title: 'قاره (اختیاری)', defaultValue: c.region || '', confirmText: 'بعدی', cancelText: 'رد' })
                          const flagEmoji = await swalPrompt({ title: 'پرچم (اختیاری)', defaultValue: c.flagEmoji || '', confirmText: 'ذخیره', cancelText: 'لغو' })
                          try {
                            await update.mutateAsync({ code2: c.code2, input: { nameFa: nameFa.trim(), nameEn: nameEn.trim(), region: region?.trim() || null, flagEmoji: flagEmoji?.trim() || null } })
                            swalToastSuccess('کشور بروزرسانی شد')
                          } catch (e:any) { swalToastError(e?.message || 'بروزرسانی ناموفق') }
                        }}>ویرایش</button>
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          try { await del.mutateAsync(c.code2); swalToastSuccess('کشور حذف شد') } catch (e:any) { swalToastError(e?.message || 'حذف ناموفق') }
                        }}>حذف</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
        <div className="card p-4">
          <h3 className="font-semibold mb-3">ایجاد کشور</h3>
          <form className="space-y-3" onSubmit={async (e) => {
            e.preventDefault();
            try {
              await create.mutateAsync({
                code2: form.code2.trim().toUpperCase(),
                code3: form.code3.trim().toUpperCase(),
                nameFa: form.nameFa.trim(),
                nameEn: form.nameEn.trim(),
                region: form.region?.trim() || null,
                flagEmoji: form.flagEmoji?.trim() || null,
              })
              setForm({ code2: 'IR', code3: 'IRN', nameFa: '', nameEn: '', region: '', flagEmoji: '' })
              swalToastSuccess('کشور ایجاد شد')
            } catch (err:any) {
              swalToastError(err?.message || 'ایجاد ناموفق')
            }
          }}>
            <div>
              <label className="label">نام (FA)</label>
              <input className="input" value={form.nameFa} onChange={e=>setForm(f=>({ ...f, nameFa: e.target.value }))} />
            </div>
            <div>
              <label className="label">نام (EN)</label>
              <input className="input" value={form.nameEn} onChange={e=>setForm(f=>({ ...f, nameEn: e.target.value }))} />
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">کد ایزو 2</label>
                <input className="input" value={form.code2} onChange={e=>setForm(f=>({ ...f, code2: e.target.value }))} />
              </div>
              <div>
                <label className="label">کد ایزو 3</label>
                <input className="input" value={form.code3} onChange={e=>setForm(f=>({ ...f, code3: e.target.value }))} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="label">قاره (اختیاری)</label>
                <input className="input" value={form.region} onChange={e=>setForm(f=>({ ...f, region: e.target.value }))} />
              </div>
              <div>
                <label className="label">پرچم</label>
                <input className="input" value={form.flagEmoji} onChange={e=>setForm(f=>({ ...f, flagEmoji: e.target.value }))} />
              </div>
            </div>
            <button type="submit" className="btn">ایجاد</button>
          </form>
        </div>
      </div>
    </div>
  )
}

