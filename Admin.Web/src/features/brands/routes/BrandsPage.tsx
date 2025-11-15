import { useState } from 'react'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useBrands, useCreateBrand, useDeleteBrand, useRenameBrand, useCountries } from '../../products/queries'
import { swalConfirm, swalToastSuccess, swalToastError, swalPrompt } from '../../../shared/utils/swal'

export function BrandsPage() {
  const { data: brands, isLoading } = useBrands()
  const create = useCreateBrand()
  const rename = useRenameBrand()
  const del = useDeleteBrand()
  const { data: countries, isLoading: loadingCountries } = useCountries()
  const [form, setForm] = useState({ name: '', countryCode: 'IR', website: '' })

  return (
    <div className="space-y-4">
      <PageHeader title="برندها">مدیریت برندها</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm">
                <thead>
                  <tr className="bg-gray-100 text-left">
                    <th className="p-2">نام</th>
                    <th className="p-2">کشور</th>
                    <th className="p-2">وضعیت</th>
                    <th className="p-2">محصولات</th>
                    <th className="p-2">عملیات</th>
                  </tr>
                </thead>
                <tbody>
                  {brands?.map(b => (
                    <tr key={b.id} className="border-b last:border-0">
                      <td className="p-2">{b.name}</td>
                      <td className="p-2">{b.countryCode}</td>
                      <td className="p-2">{String(b.status)}</td>
                      <td className="p-2">{b.productsCount}</td>
                      <td className="p-2 flex gap-2">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          const name = prompt('نام جدید', b.name)
                          if (!name) return
                          try { await rename.mutateAsync({ id: b.id, name }) } catch (e:any) { alert(e?.message || 'خطا در تغییر نام') }
                        }}>تغییر نام</button>
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={async () => {
                          try { await del.mutateAsync(b.id) } catch (e:any) { alert(e?.message || 'خطا در حذف') }
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
          <h3 className="font-semibold mb-3">ایجاد برند</h3>
          <form className="space-y-3" onSubmit={async (e) => {
            e.preventDefault();
            try {
              await create.mutateAsync({ ...form, website: form.website || null })
              setForm({ name: '', countryCode: 'IR', website: '' })
              alert('برند ایجاد شد')
            } catch (err:any) {
              alert(err?.message || 'خطا در ایجاد برند')
            }
          }}>
            <div>
              <label className="label">Country</label>
              <div className="border rounded p-2 max-h-64 overflow-auto space-y-1 text-sm">
                {loadingCountries ? (
                  <div className="p-2"><Spinner /></div>
                ) : (
                  countries?.map(c => (
                    <label key={c.code2} className="flex items-center gap-2">
                      <input type="radio" name="brandCountry" value={c.code2}
                             checked={(form.countryCode || '').toUpperCase() === c.code2}
                             onChange={()=>setForm(f=>({ ...f, countryCode: c.code2 }))} />
                      <span>{c.nameFa} ({c.code2})</span>
                    </label>
                  ))
                )}
              </div>
            </div>
            <div>
              <label className="label">نام</label>
              <input className="input" value={form.name} onChange={e=>setForm(f=>({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">کشور (ISO2)</label>
              <input className="input" value={form.countryCode} onChange={e=>setForm(f=>({ ...f, countryCode: e.target.value }))} />
            </div>
            <div>
              <label className="label">وب‌سایت</label>
              <input className="input" value={form.website} onChange={e=>setForm(f=>({ ...f, website: e.target.value }))} />
            </div>
            <button type="submit" className="btn">ایجاد</button>
          </form>
        </div>
      </div>
    </div>
  )
}
