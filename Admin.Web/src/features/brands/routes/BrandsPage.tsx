import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageHeader } from '../../../shared/components/PageHeader'
import { Spinner } from '../../../shared/components/Spinner'
import { useBrands, useCreateBrand, useDeleteBrand } from '../../products/queries'
import { swalToastSuccess, swalToastError } from '../../../shared/utils/swal'
import type { BrandStatusValue } from '../../products/api'

const brandStatusOptions = [
  { value: '1', label: 'فعال' },
  { value: '2', label: 'غیر فعال' },
  { value: '3', label: 'منسوخ شده' },
] as const

type BrandStatusOptionValue = (typeof brandStatusOptions)[number]['value']

type BrandFormState = {
  name: string
  website: string
  description: string
  establishedYear: string
  status: BrandStatusOptionValue
}

const makeInitialForm = (): BrandFormState => ({
  name: '',
  website: '',
  description: '',
  establishedYear: '',
  status: brandStatusOptions[0].value,
})

export function BrandsPage() {
  const { data: brands, isLoading } = useBrands()
  const create = useCreateBrand()
  const del = useDeleteBrand()
  const navigate = useNavigate()
  const [form, setForm] = useState<BrandFormState>(() => makeInitialForm())

  return (
    <div className="space-y-4">
      <PageHeader title="Brands">برند ها</PageHeader>
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 card p-4">
          {isLoading ? <Spinner /> : (
            <div className="overflow-x-auto">
              <table className="min-w-full text-sm text-center">
                <thead>
                  <tr className="bg-gray-100">
                    <th className="p-2">نام</th>
                    <th className="p-2">وضعیت</th>
                    <th className="p-2">تعداد محصولات</th>
                    <th className="p-2">تغییرات</th>
                  </tr>
                </thead>
                <tbody>
                  {brands?.map(b => (
                    <tr key={b.id} className="border-b last:border-0">
                      <td className="p-2">{b.name}</td>
                      <td className="p-2">{String(b.status)}</td>
                      <td className="p-2">{b.productsCount}</td>
                      <td className="p-2 brand-actions">
                        <button className="btn-secondary px-3 py-1.5 rounded" onClick={() => navigate(`/brands/${b.id}`)}>ویرایش</button>
                        <button className="btn-red px-3 py-1.5 rounded ml-2" onClick={async () => {
                          try {
                            await del.mutateAsync(b.id)
                            swalToastSuccess('برند حذف شد')
                          } catch (e:any) {
                            swalToastError(e?.message || 'خطا در حذف برند')
                          }
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
            e.preventDefault()
            const rawYear = form.establishedYear.trim()
            let establishedYear: number | null = null
            if (rawYear) {
              const parsedYear = Number.parseInt(rawYear, 10)
              if (Number.isNaN(parsedYear)) {
                swalToastError('سالت ساخت نامعتبر')
                return
              }
              establishedYear = parsedYear
            }
            const payload = {
              name: form.name.trim(),
              website: form.website.trim() || null,
              description: form.description.trim() || null,
              establishedYear,
              status: Number(form.status) as BrandStatusValue,
            }
            try {
              await create.mutateAsync(payload)
              setForm(makeInitialForm())
              swalToastSuccess('برند ایجاد شد')
            } catch (err:any) {
              swalToastError(err?.message || 'خطا در ایجاد برند')
            }
          }}>
            <div>
              <label className="label">نام</label>
              <input className="input" value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))} />
            </div>
            <div>
              <label className="label">وب سایت</label>
              <input className="input" value={form.website} onChange={e => setForm(f => ({ ...f, website: e.target.value }))} />
            </div>
            <div>
              <label className="label">توضیحات</label>
              <textarea className="input min-h-[96px]" value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div>
                <label className="label">سال تاسیس</label>
                <input className="input" type="number" min="1800" max="3000" value={form.establishedYear} onChange={e => setForm(f => ({ ...f, establishedYear: e.target.value }))} />
              </div>
            </div>
            <div>
              <label className="label">وضعیت</label>
              <select className="input" value={form.status} onChange={e => setForm(f => ({ ...f, status: e.target.value as BrandStatusOptionValue }))}>
                {brandStatusOptions.map(opt => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </div>
            <button type="submit" className="btn w-full">ایجاد</button>
          </form>
        </div>
      </div>
    </div>
  )
}
